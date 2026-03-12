namespace PlaceNamesBlazor.Configuration;

/// <summary>Builds PostgreSQL connection string: Local (ConnectionStrings:Local or DB_* env) vs Remote (DATABASE_URL). Env overrides: DATABASE_URL, DB_*, Database__UseLocalDatabase, Database__Mode.</summary>
public static class DatabaseConnectionFactory
{
    private const string LocalMode = "Local";
    private const string RemoteMode = "Remote";

    /// <summary>Returns effective connection string. Mode/UseLocalDatabase and DATABASE_URL determine local vs remote.</summary>
    public static string GetConnectionString(IConfiguration configuration)
    {
        var useLocal = GetUseLocalDatabase(configuration);
        if (useLocal)
            return GetLocalConnectionString(configuration);
        return GetRemoteConnectionString(configuration);
    }

    private static bool GetUseLocalDatabase(IConfiguration configuration)
    {
        var mode = configuration["Database:Mode"]?.Trim();
        if (string.Equals(mode, RemoteMode, StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.Equals(mode, LocalMode, StringComparison.OrdinalIgnoreCase))
            return true;
        var forceLocal = configuration["Database:UseLocalDatabase"];
        if (bool.TryParse(forceLocal, out var useLocal))
            return useLocal;
        if (string.Equals(forceLocal?.Trim(), "1", StringComparison.Ordinal))
            return true;
        if (string.Equals(forceLocal?.Trim(), "0", StringComparison.Ordinal))
            return false;
        var databaseUrl = configuration["DATABASE_URL"]?.Trim();
        if (!string.IsNullOrEmpty(databaseUrl) && !databaseUrl.StartsWith("#", StringComparison.Ordinal))
            return false;
        return true;
    }

    private static string GetLocalConnectionString(IConfiguration configuration)
    {
        var localCs = configuration.GetConnectionString("Local");
        if (!string.IsNullOrWhiteSpace(localCs))
            return localCs.Trim();

        var defaultCs = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(defaultCs))
            return defaultCs.Trim();

        var host = configuration["DB_HOST"] ?? "localhost";
        var database = configuration["DB_NAME"] ?? "place_names_db";
        var user = configuration["DB_USER"] ?? "postgres";
        var password = configuration["DB_PASSWORD"] ?? "";
        var port = configuration["DB_PORT"] ?? "5432";
        var sslMode = configuration["DB_SSLMODE"] ?? (host == "localhost" ? "Prefer" : "Require");

        return $"Host={host};Database={database};Username={user};Password={password};Port={port};Ssl Mode={sslMode}";
    }

    private static string GetRemoteConnectionString(IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"]?.Trim();
        if (string.IsNullOrWhiteSpace(databaseUrl) || databaseUrl.StartsWith("#", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Database mode is Remote but DATABASE_URL is not set. Set DATABASE_URL (e.g. from Render) or set Database:UseLocalDatabase=true / Database:Mode=Local for local.");

        return ParsePostgresUrlToNpgsql(databaseUrl);
    }

    /// <summary>Parses postgresql:// URL into Npgsql connection string.</summary>
    private static string ParsePostgresUrlToNpgsql(string url)
    {
        try
        {
            var uri = new Uri(url);
            if (uri.Scheme != "postgresql" && uri.Scheme != "postgres")
                throw new ArgumentException("DATABASE_URL must be a postgresql:// URL.", nameof(url));

            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');
            if (string.IsNullOrEmpty(database))
                database = "place_names_db";

            var user = uri.UserInfo;
            var password = "";
            if (!string.IsNullOrEmpty(user))
            {
                var colonIndex = user.IndexOf(':');
                if (colonIndex >= 0)
                {
                    password = user[(colonIndex + 1)..];
                    user = user[..colonIndex];
                    password = Uri.UnescapeDataString(password);
                }
                user = Uri.UnescapeDataString(user);
            }

            var query = uri.Query?.TrimStart('?');
            var sslMode = "Require";
            if (!string.IsNullOrEmpty(query))
            {
                var pairs = query.Split('&');
                foreach (var pair in pairs)
                {
                    var kv = pair.Split('=', 2, StringSplitOptions.None);
                    if (kv.Length == 2 && string.Equals(kv[0].Trim(), "sslmode", StringComparison.OrdinalIgnoreCase))
                    {
                        sslMode = kv[1].Trim();
                        break;
                    }
                }
            }

            return $"Host={host};Port={port};Database={database};Username={user};Password={password};Ssl Mode={sslMode}";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Invalid DATABASE_URL. Expected postgresql://user:password@host:port/database", ex);
        }
    }
}
