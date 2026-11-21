namespace KeranjangHijau.Core.Dtos;

/// <summary>Konversi setara yang ditampilkan di UI, mis. "setara 12,5 km naik motor".</summary>
/// <param name="Aktivitas">Nama aktivitas, mis. "Naik motor".</param>
/// <param name="Satuan">Satuan nilai setara, mis. "km".</param>
/// <param name="NilaiSetara">Jumlah satuan yang emisinya sama dengan emisi paket (2 desimal).</param>
/// <param name="Ikon">Nama ikon untuk UI; <c>null</c> jika tidak ada.</param>
/// <param name="IsPlaceholder">
/// <c>true</c> jika angka faktor masih sementara (sumber diawali "PLACEHOLDER"); UI menampilkan tanda "angka sementara".
/// </param>
public sealed record EquivalenceDto(
    string Aktivitas,
    string Satuan,
    decimal NilaiSetara,
    string? Ikon,
    bool IsPlaceholder);
