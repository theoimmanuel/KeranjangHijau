using System.Data;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;

namespace KeranjangHijau.Tests.Fakes;

/// <summary><see cref="IEmissionRecordRepository"/> di memori.</summary>
public sealed class InMemoryEmissionRecordRepository : IEmissionRecordRepository
{
    private readonly List<EmissionRecord> _records = [];
    private int _nextId = 1;

    public IReadOnlyList<EmissionRecord> Records => _records;

    public List<IDbTransaction?> InsertTransactions { get; } = [];

    public Task<int> InsertAsync(EmissionRecord record, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        InsertTransactions.Add(transaction);
        if (_records.Any(r => r.ShipmentId == record.ShipmentId))
        {
            throw new InvalidOperationException($"Shipment {record.ShipmentId} sudah memiliki hasil hitung emisi.");
        }

        var copy = new EmissionRecord
        {
            RecordId = _nextId++,
            ShipmentId = record.ShipmentId,
            FactorId = record.FactorId,
            JarakKm = record.JarakKm,
            TotalCo2eKg = record.TotalCo2eKg,
            Metode = record.Metode,
            CalculatedAt = record.CalculatedAt,
        };
        _records.Add(copy);
        return Task.FromResult(copy.RecordId);
    }

    public Task<EmissionRecord?> GetByIdAsync(int recordId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_records.SingleOrDefault(r => r.RecordId == recordId));
}
