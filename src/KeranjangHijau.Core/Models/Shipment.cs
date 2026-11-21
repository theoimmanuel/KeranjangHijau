using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Models;

/// <summary>Paket yang diinput pengguna dari nomor resi (tabel <c>shipments</c>).</summary>
public sealed class Shipment
{
    /// <summary>Panjang maksimum kolom <c>shipments.layanan</c> (<c>VARCHAR(50)</c>).</summary>
    public const int MaxLayananLength = 50;

    public int ShipmentId { get; set; }

    public int UserId { get; set; }

    public int CourierId { get; set; }

    /// <summary>Nomor resi bentuk baku (tanpa spasi, huruf besar).</summary>
    public string NomorResi { get; set; } = string.Empty;

    public int OriginCityId { get; set; }

    public int DestCityId { get; set; }

    /// <summary>Nama layanan kurir; <c>null</c> jika tidak tersedia (dianggap reguler).</summary>
    public string? Layanan { get; set; }

    public decimal BeratKg { get; set; }

    public ShipmentDataSource SumberData { get; set; }

    /// <summary>Waktu input (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
