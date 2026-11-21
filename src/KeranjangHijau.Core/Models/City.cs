namespace KeranjangHijau.Core.Models;

/// <summary>Master kota (tabel <c>cities</c>): koordinat untuk jarak, pulau untuk menebak moda.</summary>
public sealed class City
{
    public int CityId { get; set; }

    public string NamaKota { get; set; } = string.Empty;

    public string Provinsi { get; set; } = string.Empty;

    /// <summary>Nama pulau (mis. <c>Jawa</c>, <c>Sumatra</c>); dipakai menentukan moda (CLAUDE.md 7.3).</summary>
    public string Pulau { get; set; } = string.Empty;

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }
}
