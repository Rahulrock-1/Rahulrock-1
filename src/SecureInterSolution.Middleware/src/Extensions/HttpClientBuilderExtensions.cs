using System;
using Microsoft.Extensions.DependencyInjection;
using SecureInterSolution.Middleware.Http;
using SecureInterSolution.Middleware.Options;
using SecureInterSolution.Middleware.Crypto;
using Microsoft.Extensions.Options;

namespace SecureInterSolution.Middleware.Extensions
{
  public static class HttpClientBuilderExtensions
  {
    public static IHttpClientBuilder AddEncryptedHandler(this IHttpClientBuilder builder, string targetSolutionId, string? keyId = null)
    {
      return builder.AddHttpMessageHandler(sp => new EncryptedHttpMessageHandler(
        sp.GetRequiredService<IAeadEncryptor>(),
        sp.GetRequiredService<IOptions<SecureCommunicationOptions>>(),
        targetSolutionId,
        keyId));
    }
  }
}

