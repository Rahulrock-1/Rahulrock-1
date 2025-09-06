using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SecureInterSolution.Middleware.Crypto;
using SecureInterSolution.Middleware.Options;

namespace SecureInterSolution.Middleware.Middleware
{
  public sealed class EncryptedCommunicationMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly IAeadEncryptor _encryptor;
    private readonly IOptions<SecureCommunicationOptions> _options;

    public EncryptedCommunicationMiddleware(RequestDelegate next, IAeadEncryptor encryptor, IOptions<SecureCommunicationOptions> options)
    {
      _next = next;
      _encryptor = encryptor;
      _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      var opts = _options.Value;

      using var timeoutCts = new CancellationTokenSource(opts.RequestTimeout);
      using var abortRegistration = timeoutCts.Token.Register(context.Abort);

      bool requestIsEncrypted = context.Request.Headers.TryGetValue(opts.EncryptedFlagHeader, out var encryptedFlag) && string.Equals(encryptedFlag, "1", StringComparison.Ordinal);

      if (requestIsEncrypted)
      {
        await DecryptIncomingRequestAsync(context, opts);
      }

      // Capture response if we need to mirror-encrypt it
      Stream originalResponseBody = context.Response.Body;
      MemoryStream? responseBuffer = null;
      if (opts.MirrorEncryptResponse && requestIsEncrypted)
      {
        responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;
      }

      try
      {
        await _next(context);
      }
      catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
      {
        // Timed out
        if (!context.Response.HasStarted)
        {
          context.Response.Clear();
          context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
          await context.Response.WriteAsync("Request timed out.");
        }
        return;
      }
      finally
      {
        abortRegistration.Dispose();
      }

      if (responseBuffer != null)
      {
        try
        {
          await EncryptOutgoingResponseAsync(context, opts, responseBuffer, originalResponseBody);
        }
        finally
        {
          responseBuffer.Dispose();
          context.Response.Body = originalResponseBody;
        }
      }
    }

    private async Task DecryptIncomingRequestAsync(HttpContext context, SecureCommunicationOptions opts)
    {
      if (!context.Request.Headers.TryGetValue(opts.KeyIdHeader, out var keyIdValues))
      {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Missing key id header.");
        throw new OperationCanceledException("Missing key id header");
      }
      string keyId = keyIdValues.ToString();
      if (!opts.KeyIdToAesKey.TryGetValue(keyId, out var key))
      {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Unknown key id.");
        throw new OperationCanceledException("Unknown key id");
      }

      string originalContentType = context.Request.Headers.TryGetValue(opts.OriginalContentTypeHeader, out var oct) ? oct.ToString() : "application/octet-stream";

      using var ms = new MemoryStream();
      await context.Request.Body.CopyToAsync(ms);
      var packedBytes = ms.ToArray();
      var payload = EncryptedPayload.FromPackedBytes(packedBytes, opts.NonceLengthBytes, opts.TagLengthBytes);

      // Associated data can include routing headers to bind decryption to who/where
      string from = context.Request.Headers.TryGetValue(opts.FromSolutionHeader, out var fromV) ? fromV.ToString() : string.Empty;
      string to = context.Request.Headers.TryGetValue(opts.ToSolutionHeader, out var toV) ? toV.ToString() : string.Empty;
      string aadString = string.Concat(keyId, "|", from, "|", to);
      byte[] aad = Encoding.UTF8.GetBytes(aadString);

      byte[] plaintext = _encryptor.Decrypt(payload, aad, key);

      // Replace request body and restore content type
      context.Request.Body = new MemoryStream(plaintext);
      context.Request.ContentLength = plaintext.Length;
      context.Request.ContentType = originalContentType;
    }

    private async Task EncryptOutgoingResponseAsync(HttpContext context, SecureCommunicationOptions opts, MemoryStream responseBuffer, Stream originalResponseBody)
    {
      var bodyBytes = responseBuffer.ToArray();
      responseBuffer.Position = 0;

      if (!context.Request.Headers.TryGetValue(opts.KeyIdHeader, out var keyIdValues))
      {
        // If no key id, just write plain response
        await responseBuffer.CopyToAsync(originalResponseBody);
        return;
      }
      string keyId = keyIdValues.ToString();
      if (!opts.KeyIdToAesKey.TryGetValue(keyId, out var key))
      {
        await responseBuffer.CopyToAsync(originalResponseBody);
        return;
      }

      string originalContentType = context.Response.ContentType ?? "application/octet-stream";
      string from = context.Request.Headers.TryGetValue(opts.FromSolutionHeader, out var fromV) ? fromV.ToString() : opts.ThisSolutionId;
      string to = context.Request.Headers.TryGetValue(opts.ToSolutionHeader, out var toV) ? toV.ToString() : string.Empty;
      string aadString = string.Concat(keyId, "|", from, "|", to);
      byte[] aad = Encoding.UTF8.GetBytes(aadString);

      var payload = _encryptor.Encrypt(bodyBytes, aad, key, opts.NonceLengthBytes, opts.TagLengthBytes);
      byte[] packed = payload.ToPackedBytes();

      context.Response.Headers[opts.EncryptedFlagHeader] = "1";
      context.Response.Headers[opts.KeyIdHeader] = keyId;
      context.Response.Headers[opts.OriginalContentTypeHeader] = originalContentType;
      context.Response.ContentType = "application/octet-stream";
      context.Response.ContentLength = packed.Length;

      await originalResponseBody.WriteAsync(packed, 0, packed.Length);
    }
  }
}

