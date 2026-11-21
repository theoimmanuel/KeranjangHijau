namespace KeranjangHijau.Core.Enums;

/// <summary>Asal data paket dari tracker resi (CLAUDE.md 7.10).</summary>
public enum TrackingSource
{
    /// <summary>Data dari API cek resi pihak ketiga.</summary>
    Api,

    /// <summary>Data cadangan dari <c>MockData/mock-resi.json</c> (development dan demo).</summary>
    Mock,
}
