using System.Data;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Tests.Fakes;

/// <summary><see cref="ICourierRepository"/> di memori, berisi kurir yang sama dengan seed.sql.</summary>
public sealed class InMemoryCourierRepository : ICourierRepository
{
    private readonly List<Courier> _couriers =
    [
        new() { CourierId = 1, KodeKurir = "jne", NamaKurir = "JNE" },
        new() { CourierId = 2, KodeKurir = "jnt", NamaKurir = "J&T Express" },
        new() { CourierId = 3, KodeKurir = "sicepat", NamaKurir = "SiCepat Ekspres" },
        new() { CourierId = 4, KodeKurir = "anteraja", NamaKurir = "AnterAja" },
        new() { CourierId = 5, KodeKurir = "pos", NamaKurir = "Pos Indonesia" },
    ];

    public Task<IReadOnlyList<Courier>> GetAllAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Courier>>(_couriers.OrderBy(c => c.NamaKurir, StringComparer.Ordinal).ToList());

    public Task<Courier?> GetByKodeAsync(string kodeKurir, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var kode = kodeKurir.Trim().ToLowerInvariant();
        return Task.FromResult(_couriers.SingleOrDefault(c => c.KodeKurir == kode));
    }
}
