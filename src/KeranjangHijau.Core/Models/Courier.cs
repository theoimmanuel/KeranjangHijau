namespace KeranjangHijau.Core.Models;

/// <summary>Master kurir (tabel <c>couriers</c>).</summary>
public sealed class Courier
{
    public int CourierId { get; set; }

    /// <summary>Kode singkat huruf kecil, mis. <c>jne</c>, <c>jnt</c>, <c>sicepat</c>.</summary>
    public string KodeKurir { get; set; } = string.Empty;

    public string NamaKurir { get; set; } = string.Empty;
}
