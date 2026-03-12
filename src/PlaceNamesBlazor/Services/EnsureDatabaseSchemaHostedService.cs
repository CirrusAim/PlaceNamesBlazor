using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PlaceNamesBlazor.Data;

namespace PlaceNamesBlazor.Services;

/// <summary>Ensures full DB schema at startup (tables, indexes, seed). Idempotent. DB must exist (e.g. createdb place_names_db).</summary>
public class EnsureDatabaseSchemaHostedService : IHostedService
{
    private readonly IServiceProvider _services;

    public EnsureDatabaseSchemaHostedService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PlaceNamesDbContext>>();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var script = LoadEmbeddedSchemaScript();
        if (string.IsNullOrWhiteSpace(script))
            throw new InvalidOperationException("Embedded resource PlaceNamesBlazor.scripts.full_schema.sql not found. Ensure scripts\\full_schema.sql is set as EmbeddedResource.");

        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync(cancellationToken);

        try
        {
            if (conn is NpgsqlConnection npgsqlConn)
            {
                await using var cmd = new NpgsqlCommand(script, npgsqlConn) { CommandTimeout = 120 };
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static string LoadEmbeddedSchemaScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "PlaceNamesBlazor.scripts.full_schema.sql";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return string.Empty;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
