using System.Collections.Generic;

namespace MultiDb.Extensions.Options
{
    public sealed class MultiDbOptions
    {
        public const string SectionName = "MultiDb";

        public IList<DatabaseConfig> Databases { get; init; } = new List<DatabaseConfig>();
    }

    public sealed class DatabaseConfig
    {
        public string Name { get; init; } = string.Empty;

        // e.g. "SqlServer", "PostgreSql", "MySql", "Sqlite".
        public string Provider { get; init; } = string.Empty;

        public string ConnectionString { get; init; } = string.Empty;

        // Optional: whether to open a warm connection on startup
        public bool WarmupOnStart { get; init; } = true;
    }
}

