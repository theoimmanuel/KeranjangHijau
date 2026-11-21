using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Menebak moda transport karena tidak tercantum di data resi (CLAUDE.md 7.3).</summary>
public interface ITransportModeResolver
{
    /// <summary>
    /// Satu pulau → <see cref="TransportMode.Truk"/>; beda pulau + layanan ekspres →
    /// <see cref="TransportMode.Pesawat"/>; beda pulau + layanan lain (termasuk kosong) →
    /// <see cref="TransportMode.Kapal"/>.
    /// </summary>
    /// <param name="pulauAsal">Kolom <c>cities.pulau</c> kota asal.</param>
    /// <param name="pulauTujuan">Kolom <c>cities.pulau</c> kota tujuan.</param>
    /// <param name="layanan">Nama layanan kurir; <c>null</c>/kosong dianggap reguler.</param>
    TransportMode Resolve(string pulauAsal, string pulauTujuan, string? layanan);
}
