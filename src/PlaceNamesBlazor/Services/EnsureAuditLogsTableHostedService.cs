using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Data;

namespace PlaceNamesBlazor.Services;

/// <summary>Ensures audit_logs table exists at startup for DBs that predate it. Idempotent.</summary>
public class EnsureAuditLogsTableHostedService : IHostedService
{
    private readonly IServiceProvider _services;

    public EnsureAuditLogsTableHostedService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PlaceNamesDbContext>>();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        const string createTable = """
            CREATE TABLE IF NOT EXISTS audit_logs (
                audit_id       SERIAL PRIMARY KEY,
                actor_id       INTEGER NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
                actor_email    VARCHAR(100) NOT NULL,
                actor_role     VARCHAR(20) NOT NULL,
                action_type    VARCHAR(50) NOT NULL,
                target_type    VARCHAR(50) NULL,
                target_id      INTEGER NULL,
                target_description VARCHAR(255) NULL,
                details        JSONB NULL,
                ip_address     VARCHAR(45) NULL,
                user_agent     VARCHAR(512) NULL,
                created_at     TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
            )
            """;
        await db.Database.ExecuteSqlRawAsync(createTable, cancellationToken);

        string[] indexSql =
        [
            "CREATE INDEX IF NOT EXISTS idx_audit_logs_actor_id ON audit_logs(actor_id)",
            "CREATE INDEX IF NOT EXISTS idx_audit_logs_action_type ON audit_logs(action_type)",
            "CREATE INDEX IF NOT EXISTS idx_audit_logs_target_type ON audit_logs(target_type)",
            "CREATE INDEX IF NOT EXISTS idx_audit_logs_target_id ON audit_logs(target_id)",
            "CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at ON audit_logs(created_at)"
        ];
        foreach (var sql in indexSql)
            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);

        const string addUserAgent = """
            DO $$
            BEGIN
              IF NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'audit_logs' AND column_name = 'user_agent'
              ) THEN
                ALTER TABLE audit_logs ADD COLUMN user_agent VARCHAR(512) NULL;
              END IF;
            END $$
            """;
        await db.Database.ExecuteSqlRawAsync(addUserAgent, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS audit_logs_append_only_trigger ON audit_logs", cancellationToken);
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS audit_logs_reject_truncate_trigger ON audit_logs", cancellationToken);
        await db.Database.ExecuteSqlRawAsync("DROP FUNCTION IF EXISTS audit_logs_reject_update_delete()", cancellationToken);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION audit_logs_reject_update_delete() RETURNS TRIGGER AS $$
            BEGIN
              RAISE EXCEPTION 'audit_logs is append-only; updates and deletes are not allowed.';
              RETURN NULL;
            END;
            $$ LANGUAGE plpgsql
            """, cancellationToken);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER audit_logs_append_only_trigger BEFORE UPDATE OR DELETE ON audit_logs FOR EACH ROW EXECUTE FUNCTION audit_logs_reject_update_delete()", cancellationToken);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER audit_logs_reject_truncate_trigger BEFORE TRUNCATE ON audit_logs FOR EACH STATEMENT EXECUTE FUNCTION audit_logs_reject_update_delete()", cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
