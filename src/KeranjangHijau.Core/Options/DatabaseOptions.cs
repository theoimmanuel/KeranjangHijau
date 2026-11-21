namespace KeranjangHijau.Core.Options;

/// <summary>
/// Pengaturan koneksi database (bagian <c>Database</c> di konfigurasi).
/// Connection string asli hanya boleh berasal dari <c>appsettings.Local.json</c>
/// atau environment variable <c>KH_DB</c>, tidak pernah dari file yang di-commit.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Connection string PostgreSQL, format key-value Npgsql
    /// (<c>Host=...;Database=...</c>) atau URI (<c>postgresql://user:pass@host:5432/db</c>).
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}
