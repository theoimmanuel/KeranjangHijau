using Dapper;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>Pengaturan global Dapper; dipanggil sekali oleh <see cref="DependencyInjection"/>.</summary>
public static class DapperConfiguration
{
    private static int _configured;

    public static void Configure()
    {
        if (Interlocked.Exchange(ref _configured, 1) == 1)
        {
            return;
        }

        // Kolom snake_case (nomor_resi) otomatis dipetakan ke properti PascalCase (NomorResi).
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        // TIMESTAMPTZ <-> DateTimeOffset (UTC).
        SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
    }
}
