using System.Data;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Tests.Fakes;

/// <summary><see cref="IForestService"/> palsu untuk service lain; hanya <see cref="GetForestAsync"/> yang dipakai.</summary>
public sealed class FakeForestService : IForestService
{
    /// <summary>Hutan per user ID yang dikembalikan <see cref="GetForestAsync"/>.</summary>
    public Dictionary<int, ForestDto> Forests { get; } = [];

    public List<int> GetForestUserIds { get; } = [];

    public Task<ForestDto> GetForestAsync(int userId, CancellationToken cancellationToken = default)
    {
        GetForestUserIds.Add(userId);
        return Forests.TryGetValue(userId, out var forest)
            ? Task.FromResult(forest)
            : throw new NotFoundException("Hutan tidak ditemukan.");
    }

    public static ForestDto Sample(string nama = "Hutan Budi", decimal kesehatan = 87.5m) =>
        new(nama, kesehatan, ForestStatus.Lestari, 100, 88);

    public Task<ForestSnapshotDto> ApplyEmissionAsync(
        int userId, int recordId, string nomorResi, decimal totalCo2eKg, IDbTransaction transaction, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<ForestSnapshotDto> ApplyRecoveryAsync(int userId, IDbTransaction transaction, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<ForestLogDto>> GetForestHistoryAsync(int userId, int limit = 30, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
