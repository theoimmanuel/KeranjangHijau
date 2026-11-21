using System.Data;
using KeranjangHijau.Core.Dtos;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>Validasi input kurir + nomor resi sebelum emisi dihitung (CLAUDE.md 7.6).</summary>
public interface IResiValidator
{
    /// <summary>
    /// Menormalisasi dan memvalidasi nomor resi, memastikan kurir dikenal, lalu (jika
    /// <c>Features:AllowDuplicateResi = false</c>) memastikan resi belum pernah dicatat.
    /// </summary>
    /// <returns>Kurir yang ditemukan dan nomor resi yang sudah dinormalisasi.</returns>
    /// <exception cref="Exceptions.ValidationException">Format resi salah atau kurir tidak dikenal.</exception>
    /// <exception cref="Exceptions.DuplicateResiException">Resi sudah pernah dicatat untuk kurir yang sama.</exception>
    Task<ValidatedResi> ValidateAsync(
        string? kodeKurir,
        string? nomorResi,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}
