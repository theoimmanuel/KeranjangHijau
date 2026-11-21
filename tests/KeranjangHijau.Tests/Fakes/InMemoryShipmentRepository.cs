using System.Data;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Fakes;

/// <summary><see cref="IShipmentRepository"/> di memori.</summary>
public sealed class InMemoryShipmentRepository : IShipmentRepository
{
    private readonly HashSet<(int CourierId, string NomorResi)> _seeded = [];
    private readonly List<Shipment> _shipments = [];
    private int _nextId = 1;

    /// <summary>Jumlah pemanggilan <see cref="ExistsAsync"/> (untuk memastikan cek duplikat dilewati).</summary>
    public int ExistsCallCount { get; private set; }

    public IReadOnlyList<Shipment> Shipments => _shipments;

    public List<IDbTransaction?> InsertTransactions { get; } = [];

    /// <summary>Menandai resi seolah-olah sudah ada shipment-nya di database.</summary>
    public void Seed(int courierId, string nomorResi) => _seeded.Add((courierId, ResiNormalizer.Normalize(nomorResi)));

    public Task<bool> ExistsAsync(int courierId, string nomorResi, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ExistsCallCount++;
        var resi = ResiNormalizer.Normalize(nomorResi);
        return Task.FromResult(
            _seeded.Contains((courierId, resi)) || _shipments.Any(s => s.CourierId == courierId && s.NomorResi == resi));
    }

    public Task<int> InsertAsync(Shipment shipment, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        InsertTransactions.Add(transaction);
        var copy = new Shipment
        {
            ShipmentId = _nextId++,
            UserId = shipment.UserId,
            CourierId = shipment.CourierId,
            NomorResi = ResiNormalizer.Normalize(shipment.NomorResi),
            OriginCityId = shipment.OriginCityId,
            DestCityId = shipment.DestCityId,
            Layanan = shipment.Layanan,
            BeratKg = shipment.BeratKg,
            SumberData = shipment.SumberData,
            CreatedAt = shipment.CreatedAt,
        };
        _shipments.Add(copy);
        return Task.FromResult(copy.ShipmentId);
    }
}
