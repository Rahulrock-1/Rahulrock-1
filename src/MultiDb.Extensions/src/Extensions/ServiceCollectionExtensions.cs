using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MultiDb.Extensions.Abstractions;
using MultiDb.Extensions.Hosting;
using MultiDb.Extensions.Options;
using MultiDb.Extensions.Services;

namespace MultiDb.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers multi-database options and services. Supports configuration binding and optional warmup.
        /// Expected configuration section: "MultiDb".
        /// </summary>
        /// <param name="services">The DI service collection.</param>
        /// <param name="configuration">Optional configuration root; if provided, binds options from section "MultiDb".</param>
        /// <param name="configureOptions">Optional delegate to configure options in code.</param>
        /// <param name="addWarmupHostedService">If true, adds a hosted service to open warm connections on startup.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMultiDb(this IServiceCollection services,
            IConfiguration? configuration = null,
            Action<MultiDbOptions>? configureOptions = null,
            bool addWarmupHostedService = true)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            if (configuration is not null)
            {
                services.AddOptions<MultiDbOptions>()
                    .Bind(configuration.GetSection(MultiDbOptions.SectionName))
                    .Validate(options => options is not null, "MultiDb options must be provided.")
                    .ValidateOnStart();
            }
            else
            {
                services.AddOptions<MultiDbOptions>();
            }

            if (configureOptions is not null)
            {
                services.Configure(configureOptions);
            }

            services.AddSingleton<IMultiDbConnectionFactory, MultiDbConnectionFactory>();

            if (addWarmupHostedService)
            {
                services.AddHostedService<MultiDbWarmupHostedService>();
            }

            return services;
        }
    }
}

