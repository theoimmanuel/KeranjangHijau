using KeranjangHijau.Infrastructure;

namespace KeranjangHijau.Tests.Integration;

/// <summary>
/// <see cref="FactAttribute"/> untuk integration test ke database sungguhan.
/// Otomatis dilewati (skip) jika environment variable <c>KH_DB</c> tidak di-set.
/// </summary>
public sealed class DatabaseFactAttribute : FactAttribute
{
    public DatabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(BackendConfiguration.DbEnvironmentVariable)))
        {
            Skip = "KH_DB tidak di-set; integration test database dilewati.";
        }
    }
}
