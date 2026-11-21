using System.Data;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>
/// Titik perluasan alur hitung emisi: dipanggil setelah <c>shipments</c> dan <c>emission_records</c>
/// tersimpan, <b>di dalam transaksi yang sama</b>. Dipakai mis. untuk konversi setara (M4.1) dan
/// perubahan kesehatan hutan (M5.1) tanpa mengubah <c>EmissionService</c>.
/// </summary>
/// <remarks>
/// Daftarkan implementasi di DI sebagai <c>IEmissionRecordedHandler</c> (boleh lebih dari satu);
/// handler dipanggil berurutan sesuai urutan pendaftaran. Exception dari handler membatalkan
/// (rollback) seluruh transaksi, termasuk shipment dan record. Teruskan <paramref name="transaction"/>
/// ke setiap method repository. Untuk menambahkan hasil ke UI, ganti <see cref="EmissionRecordedContext.Result"/>
/// dengan salinan baru (<c>context.Result = context.Result with { ... }</c>); handler berikutnya melihat hasil
/// yang sudah diganti, dan <c>EmissionService</c> mengembalikan nilai terakhir.
/// </remarks>
public interface IEmissionRecordedHandler
{
    Task HandleAsync(EmissionRecordedContext context, IDbTransaction transaction, CancellationToken cancellationToken = default);
}

/// <summary>Data yang baru tersimpan, diberikan ke <see cref="IEmissionRecordedHandler"/>.</summary>
/// <param name="User">Pengguna yang menghitung (sesi login).</param>
/// <param name="Shipment">Shipment yang tersimpan (sudah berisi <c>ShipmentId</c>).</param>
/// <param name="Record">Record emisi yang tersimpan (sudah berisi <c>RecordId</c>).</param>
/// <param name="Result">Hasil awal yang akan dikembalikan ke UI.</param>
public sealed record EmissionRecordedContext(UserDto User, Shipment Shipment, EmissionRecord Record, EmissionResultDto Result)
{
    private EmissionResultDto _result = Result ?? throw new ArgumentNullException(nameof(Result));

    /// <summary>
    /// Hasil yang akan dikembalikan ke UI. Handler boleh menggantinya dengan salinan yang diperkaya
    /// (mis. konversi setara), tetapi harus tetap untuk record yang sama.
    /// </summary>
    /// <exception cref="ArgumentException">Nilai baru milik record lain.</exception>
    public EmissionResultDto Result
    {
        get => _result;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.RecordId != _result.RecordId)
            {
                throw new ArgumentException("Hasil pengganti harus untuk record emisi yang sama.", nameof(value));
            }

            _result = value;
        }
    }
}
