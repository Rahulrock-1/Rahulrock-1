using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiDb.Extensions.Abstractions;
using MultiDb.Extensions.Options;

namespace MultiDb.Extensions.Hosting
{
    internal sealed class MultiDbWarmupHostedService : IHostedService
    {
        private readonly ILogger<MultiDbWarmupHostedService> _logger;
        private readonly IMultiDbConnectionFactory _connectionFactory;
        private readonly IOptions<MultiDbOptions> _options;

        public MultiDbWarmupHostedService(
            ILogger<MultiDbWarmupHostedService> logger,
            IMultiDbConnectionFactory connectionFactory,
            IOptions<MultiDbOptions> options)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _options = options;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var configs = _options.Value?.Databases;
            if (configs is null)
            {
                return;
            }

            foreach (var cfg in configs)
            {
                if (!cfg.WarmupOnStart)
                {
                    continue;
                }

                try
                {
                    using var connection = _connectionFactory.CreateConnection(cfg.Name);
                    await OpenQuietlyAsync(connection, cancellationToken);
                    _logger.LogInformation("Warm connection established for database '{DatabaseName}'.", cfg.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Warm connection failed for database '{DatabaseName}'.", cfg.Name);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private static async Task OpenQuietlyAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Let caller decide logging; swallow to avoid failing app startup.
            }
        }
    }
}

