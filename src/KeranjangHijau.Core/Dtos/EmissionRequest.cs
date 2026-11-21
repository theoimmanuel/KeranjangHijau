using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Core.Dtos;

/// <summary>Input dari UI untuk menghitung emisi satu paket.</summary>
/// <param name="KodeKurir">Kode kurir yang dipilih pengguna, mis. <c>jne</c>.</param>
/// <param name="NomorResi">Nomor resi apa adanya (spasi dan huruf kecil dinormalisasi).</param>
/// <param name="BeratManualKg">Berat yang diisi pengguna; hanya dipakai jika data resi tidak punya berat (CLAUDE.md 7.4).</param>
public sealed record EmissionRequest(string? KodeKurir, string? NomorResi, decimal? BeratManualKg = null)
{
    /// <summary>
    /// Opsional: 5 digit terakhir nomor HP penerima (diminta sebagian kurir di API cek resi, M2.4).
    /// Kosong = tidak dikirim. Hanya diteruskan ke tracker; tidak pernah disimpan ke database.
    /// </summary>
    public string? NomorHpAkhir { get; init; }

    /// <summary>Nomor HP disamarkan supaya tidak bocor jika objek ini tidak sengaja dicatat.</summary>
    public override string ToString() =>
        $"EmissionRequest {{ KodeKurir = {KodeKurir}, NomorResi = {NomorResi}, BeratManualKg = {BeratManualKg}, NomorHpAkhir = {PhoneSuffixRule.Mask(NomorHpAkhir)} }}";
}
