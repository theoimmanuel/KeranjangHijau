using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Models;

/// <summary>Hasil hitung emisi satu shipment (tabel <c>emission_records</c>, 1 shipment = 1 record).</summary>
public sealed class EmissionRecord
{
    public int RecordId { get; set; }

    public int ShipmentId { get; set; }

    public int FactorId { get; set; }

    public decimal JarakKm { get; set; }

    public decimal TotalCo2eKg { get; set; }

    public EmissionMethod Metode { get; set; }

    /// <summary>Waktu hitung (UTC).</summary>
    public DateTimeOffset CalculatedAt { get; set; }
}
