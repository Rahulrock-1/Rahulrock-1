using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureInterSolution.Middleware.Crypto;
using SecureInterSolution.Middleware.Options;

namespace SecureInterSolution.Middleware.Extensions
{
  public static class ServiceCollectionExtensions
  {
    public static IServiceCollection AddSecureCommunication(this IServiceCollection services, Action<SecureCommunicationOptions> configure)
    {
      services.Configure(configure);
      services.AddSingleton<IAeadEncryptor, AesGcmEncryptor>();
      return services;
    }

    public static IServiceCollection AddSecureCommunication(this IServiceCollection services, IConfigurationSection section)
    {
      services.Configure<SecureCommunicationOptions>(section);
      services.AddSingleton<IAeadEncryptor, AesGcmEncryptor>();
      return services;
    }
  }
}

