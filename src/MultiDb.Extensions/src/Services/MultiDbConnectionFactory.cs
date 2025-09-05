using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiDb.Extensions.Abstractions;
using MultiDb.Extensions.Options;

namespace MultiDb.Extensions.Services
{
    public sealed class MultiDbConnectionFactory : IMultiDbConnectionFactory
    {
        private readonly ILogger<MultiDbConnectionFactory> _logger;
        private readonly IReadOnlyDictionary<string, DatabaseConfig> _databaseConfigsByName;

        public MultiDbConnectionFactory(
            IOptions<MultiDbOptions> options,
            ILogger<MultiDbConnectionFactory> logger)
        {
            _logger = logger;
            var configs = options.Value?.Databases ?? new List<DatabaseConfig>();
            _databaseConfigsByName = configs.ToDictionary(
                keySelector: c => c.Name,
                elementSelector: c => c,
                comparer: StringComparer.OrdinalIgnoreCase);
        }

        public DbConnection CreateConnection(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException("Database name must be provided", nameof(databaseName));
            }

            if (!_databaseConfigsByName.TryGetValue(databaseName, out var config))
            {
                throw new InvalidOperationException($"No database configuration was found for '{databaseName}'.");
            }

            var provider = config.Provider?.Trim();
            if (string.IsNullOrWhiteSpace(provider))
            {
                throw new InvalidOperationException($"Database '{databaseName}' has no provider configured.");
            }

            var connectionString = config.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Database '{databaseName}' has an empty connection string.");
            }

            var connection = CreateProviderConnection(provider, connectionString);
            return connection ?? throw new NotSupportedException($"Provider '{provider}' is not supported.");
        }

        private DbConnection? CreateProviderConnection(string provider, string connectionString)
        {
            // Provider-specific implementations are dynamically located via DbProviderFactories when available.
            // Consumers can ensure provider registrations are available by referencing provider packages.
            try
            {
                // Try canonical invariant names first (e.g., Npgsql, MySql.Data.MySqlClient, System.Data.SqlClient, Microsoft.Data.SqlClient)
                var invariantNamesToTry = provider switch
                {
                    "PostgreSql" or "Postgres" or "Npgsql" => new[] { "Npgsql" },
                    "SqlServer" or "MSSQL" => new[] { "Microsoft.Data.SqlClient", "System.Data.SqlClient" },
                    "MySql" => new[] { "MySql.Data.MySqlClient" },
                    "Sqlite" or "SQLite" => new[] { "Microsoft.Data.Sqlite" },
                    _ => new[] { provider }
                };

                foreach (var invariant in invariantNamesToTry)
                {
                    var factory = GetFactorySafe(invariant);
                    if (factory is null)
                    {
                        continue;
                    }
                    var connection = factory.CreateConnection();
                    if (connection is null)
                    {
                        continue;
                    }
                    connection.ConnectionString = connectionString;
                    return connection;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create provider connection for provider '{Provider}'.", provider);
            }

            return null;
        }

        private static DbProviderFactory? GetFactorySafe(string providerInvariantName)
        {
            try
            {
                return DbProviderFactories.GetFactory(providerInvariantName);
            }
            catch
            {
                return null;
            }
        }
    }
}

