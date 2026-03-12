using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Data;

namespace PlaceNamesBlazor.Services;

/// <summary>Ensures rapportoer has status column at startup for DBs that predate it. Idempotent.</summary>
public class EnsureRapportoerSchemaHostedService : IHostedService
{
    private readonly IServiceProvider _services;

    public EnsureRapportoerSchemaHostedService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PlaceNamesDbContext>>();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        const string sql = """
            DO $$
            BEGIN
              IF NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = 'rapportoer' AND column_name = 'status'
              ) THEN
                ALTER TABLE rapportoer ADD COLUMN status VARCHAR(20) NULL;
                UPDATE rapportoer SET status = 'approved' WHERE status IS NULL;
                ALTER TABLE rapportoer ALTER COLUMN status SET NOT NULL;
                ALTER TABLE rapportoer ALTER COLUMN status SET DEFAULT 'pending';
              END IF;
            END $$;
            """;

        await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
