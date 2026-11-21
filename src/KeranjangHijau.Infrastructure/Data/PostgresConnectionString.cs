using Npgsql;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>
/// Menyeragamkan connection string PostgreSQL ke format key-value Npgsql.
/// Format URI (<c>postgresql://user:pass@host:5432/db?sslmode=require</c>) diterima supaya
/// nilai <c>KH_DB</c> yang sama bisa dipakai oleh <c>psql</c> dan oleh aplikasi.
/// </summary>
public static class PostgresConnectionString
{
    public const string MissingMessage =
        "Connection string database belum diatur. Isi Database:ConnectionString di " +
        "appsettings.Local.json atau set environment variable KH_DB.";

    private const int DefaultPort = 5432;

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(MissingMessage);
        }

        var trimmed = value.Trim();
        return IsUri(trimmed) ? FromUri(trimmed) : trimmed;
    }

    private static bool IsUri(string value) =>
        value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase);

    private static string FromUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            throw new InvalidOperationException("Format URI connection string database tidak valid.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : DefaultPort,
        };

        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        if (database.Length > 0)
        {
            builder.Database = database;
        }

        if (uri.UserInfo.Length > 0)
        {
            var parts = uri.UserInfo.Split(':', 2);
            builder.Username = Uri.UnescapeDataString(parts[0]);
            if (parts.Length == 2)
            {
                builder.Password = Uri.UnescapeDataString(parts[1]);
            }
        }

        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(kv[0]);
            var val = kv.Length == 2 ? Uri.UnescapeDataString(kv[1]) : string.Empty;
            try
            {
                builder[MapQueryKey(key)] = val;
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException($"Parameter '{key}' pada connection string database tidak dikenali.", ex);
            }
        }

        return builder.ConnectionString;
    }

    private static string MapQueryKey(string key) => key.ToLowerInvariant() switch
    {
        "sslmode" => "SSL Mode",
        "application_name" => "Application Name",
        "connect_timeout" => "Timeout",
        _ => key,
    };
}
