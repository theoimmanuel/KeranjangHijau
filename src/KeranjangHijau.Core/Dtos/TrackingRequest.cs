using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Core.Dtos;

/// <summary>Permintaan pelacakan satu resi ke <see cref="Interfaces.ICourierTracker"/>.</summary>
/// <param name="KodeKurir">Kode kurir, mis. <c>jne</c> (tidak peka huruf besar/kecil).</param>
/// <param name="NomorResi">Nomor resi (spasi dan huruf kecil dinormalisasi oleh tracker).</param>
/// <param name="NomorHpAkhir">
/// Opsional: 5 digit terakhir nomor HP penerima, diwajibkan sebagian kurir di API cek resi.
/// Data pribadi: tidak boleh disimpan ke database, dicatat di log, atau dimasukkan ke pesan error.
/// </param>
public sealed record TrackingRequest(string KodeKurir, string NomorResi, string? NomorHpAkhir = null)
{
    /// <summary>
    /// Salinan dalam bentuk baku: kode kurir huruf kecil, resi tanpa spasi + huruf besar, dan
    /// nomor HP hanya dipertahankan jika tepat 5 digit (selain itu dianggap tidak ada).
    /// </summary>
    public TrackingRequest Normalize()
    {
        ArgumentNullException.ThrowIfNull(KodeKurir);
        ArgumentNullException.ThrowIfNull(NomorResi);

        var hp = PhoneSuffixRule.Normalize(NomorHpAkhir);
        return new TrackingRequest(
            KodeKurir.Trim().ToLowerInvariant(),
            ResiNormalizer.Normalize(NomorResi),
            hp is not null && PhoneSuffixRule.IsValid(hp) ? hp : null);
    }

    /// <summary>Nomor HP disamarkan supaya tidak bocor jika objek ini tidak sengaja dicatat.</summary>
    public override string ToString() =>
        $"TrackingRequest {{ KodeKurir = {KodeKurir}, NomorResi = {NomorResi}, NomorHpAkhir = {PhoneSuffixRule.Mask(NomorHpAkhir)} }}";
}
