# MultiDb.Extensions

Register multiple database configurations with a single extension method, and optionally warm up connections on startup.

## Install

```bash
dotnet add package MultiDb.Extensions
```

## Configure

appsettings.json:

```json
{
  "MultiDb": {
    "Databases": [
      {
        "Name": "Primary",
        "Provider": "SqlServer",
        "ConnectionString": "Server=.;Database=App;Trusted_Connection=True;TrustServerCertificate=True",
        "WarmupOnStart": true
      },
      {
        "Name": "Reporting",
        "Provider": "PostgreSql",
        "ConnectionString": "Host=localhost;Username=postgres;Password=postgres;Database=reporting",
        "WarmupOnStart": false
      }
    ]
  }
}
```

Program.cs:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Bind from configuration and warmup on start
builder.Services.AddMultiDb(builder.Configuration, addWarmupHostedService: true);

var app = builder.Build();
app.Run();
```

Or configure in code:

```csharp
builder.Services.AddMultiDb(configureOptions: options =>
{
    options.Databases.Add(new DatabaseConfig
    {
        Name = "Primary",
        Provider = "Npgsql",
        ConnectionString = builder.Configuration.GetConnectionString("Primary")!
    });
});
```

## Usage

```csharp
public sealed class MyRepository(IMultiDbConnectionFactory factory)
{
    public async Task<int> GetCountAsync(CancellationToken ct)
    {
        await using var conn = factory.CreateConnection("Primary");
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1";
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }
}
```

Note: Ensure your app references relevant ADO.NET providers: `Npgsql`, `Microsoft.Data.SqlClient`, `MySql.Data`, `Microsoft.Data.Sqlite`.

## DI Auto-Registration (Attributes)

Use attributes to auto-register services with desired lifetimes. Then call `AddAttributedServicesFromAppDomain()` or `AddAttributedServices()`.

```csharp
using MultiDb.Extensions.DI.Attributes;

[RegisterSingleton(typeof(IMySingletonService))]
public sealed class MySingletonService : IMySingletonService { }

[RegisterScoped(typeof(IMyScopedService))]
public sealed class MyScopedService : IMyScopedService { }

[RegisterTransient(typeof(IMyTransientService))]
public sealed class MyTransientService : IMyTransientService { }

// Program.cs
builder.Services.AddAttributedServicesFromAppDomain();
```

## Pack / Publish

```bash
dotnet pack -c Release /p:PackageVersion=1.0.0
# then push
# dotnet nuget push ./bin/Release/*.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

License: MIT