using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SecureInterSolution.Middleware.Crypto;
using SecureInterSolution.Middleware.Options;

namespace SecureInterSolution.Middleware.Http
{
  public sealed class EncryptedHttpMessageHandler : DelegatingHandler
  {
    private readonly IAeadEncryptor _encryptor;
    private readonly IOptions<SecureCommunicationOptions> _options;
    private readonly string _targetSolutionId;
    private readonly string _keyId;

    public EncryptedHttpMessageHandler(IAeadEncryptor encryptor, IOptions<SecureCommunicationOptions> options, string targetSolutionId, string? keyId = null)
    {
      _encryptor = encryptor;
      _options = options;
      _targetSolutionId = targetSolutionId;
      _keyId = keyId ?? options.Value.DefaultKeyId;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      var opts = _options.Value;

      if (!opts.KeyIdToAesKey.TryGetValue(_keyId, out var key))
      {
        throw new InvalidOperationException($"Unknown key id '{_keyId}'.");
      }

      string originalContentType = request.Content?.Headers.ContentType?.ToString() ?? "application/octet-stream";
      byte[] payloadBytes = request.Content == null ? Array.Empty<byte>() : await request.Content.ReadAsByteArrayAsync(cancellationToken);

      string aadString = string.Concat(_keyId, "|", opts.ThisSolutionId, "|", _targetSolutionId);
      byte[] aad = Encoding.UTF8.GetBytes(aadString);

      var encrypted = _encryptor.Encrypt(payloadBytes, aad, key, opts.NonceLengthBytes, opts.TagLengthBytes);
      byte[] packed = encrypted.ToPackedBytes();

      request.Headers.Remove(opts.EncryptedFlagHeader);
      request.Headers.Remove(opts.KeyIdHeader);
      request.Headers.Remove(opts.FromSolutionHeader);
      request.Headers.Remove(opts.ToSolutionHeader);
      request.Headers.Remove(opts.OriginalContentTypeHeader);

      request.Headers.Add(opts.EncryptedFlagHeader, "1");
      request.Headers.Add(opts.KeyIdHeader, _keyId);
      request.Headers.Add(opts.FromSolutionHeader, opts.ThisSolutionId);
      request.Headers.Add(opts.ToSolutionHeader, _targetSolutionId);
      request.Headers.Add(opts.OriginalContentTypeHeader, originalContentType);

      request.Content = new ByteArrayContent(packed);
      request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
      request.Content.Headers.ContentLength = packed.Length;

      using var timeoutCts = new CancellationTokenSource(opts.RequestTimeout);
      using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
      try
      {
        var response = await base.SendAsync(request, linked.Token);
        if (response.Headers.Contains(opts.EncryptedFlagHeader))
        {
          // Decrypt response
          if (!response.Headers.TryGetValues(opts.KeyIdHeader, out var keyIdValues))
          {
            throw new InvalidOperationException("Encrypted response missing key id header");
          }
          string respKeyId = System.Linq.Enumerable.First(keyIdValues);
          if (!opts.KeyIdToAesKey.TryGetValue(respKeyId, out var respKey))
          {
            throw new InvalidOperationException("Unknown response key id");
          }

          byte[] respBytes = await response.Content.ReadAsByteArrayAsync(linked.Token);
          var payload = EncryptedPayload.FromPackedBytes(respBytes, opts.NonceLengthBytes, opts.TagLengthBytes);

          string aadRespString = string.Concat(respKeyId, "|", _targetSolutionId, "|", opts.ThisSolutionId);
          byte[] aadResp = Encoding.UTF8.GetBytes(aadRespString);
          byte[] plaintext = _encryptor.Decrypt(payload, aadResp, respKey);

          string contentType = response.Headers.TryGetValues(opts.OriginalContentTypeHeader, out var octValues) ? System.Linq.Enumerable.First(octValues) : "application/octet-stream";

          response.Content = new ByteArrayContent(plaintext);
          response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
          response.Content.Headers.ContentLength = plaintext.Length;
          response.Headers.Remove(opts.EncryptedFlagHeader);
          response.Headers.Remove(opts.OriginalContentTypeHeader);
        }
        return response;
      }
      catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
      {
        throw new TimeoutException("Request timed out after " + opts.RequestTimeout.TotalSeconds + " seconds.");
      }
    }
  }
}

