using System.Data;
using Dapper;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using Npgsql;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>Implementasi <see cref="IEmissionRecordRepository"/> dengan Dapper (selalu query berparameter).</summary>
public sealed class EmissionRecordRepository : DapperRepositoryBase, IEmissionRecordRepository
{
    public const string ShipmentNotFoundMessage = "Data paket tidak ditemukan.";

    private readonly IClock _clock;

    public EmissionRecordRepository(IDbConnectionFactory connectionFactory, IClock clock)
        : base(connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    public async Task<int> InsertAsync(
        EmissionRecord record, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        const string sql = """
            INSERT INTO emission_records (shipment_id, factor_id, jarak_km, total_co2e_kg, metode, calculated_at)
            VALUES (@ShipmentId, @FactorId, @JarakKm, @TotalCo2eKg, @Metode, @CalculatedAt)
            RETURNING record_id
            """;

        var parameters = new
        {
            record.ShipmentId,
            record.FactorId,
            record.JarakKm,
            record.TotalCo2eKg,
            Metode = record.Metode.ToDbValue(),
            CalculatedAt = (record.CalculatedAt == default ? _clock.UtcNow : record.CalculatedAt).ToUniversalTime(),
        };

        try
        {
            return await RunAsync(
                transaction,
                conn => conn.ExecuteScalarAsync<int>(Command(sql, parameters, transaction, cancellationToken)),
                cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException ex) when (PostgresErrors.IsUniqueViolation(ex, "uq_emission_records_shipment"))
        {
            throw new InvalidOperationException($"Shipment {record.ShipmentId} sudah memiliki hasil hitung emisi.", ex);
        }
        catch (PostgresException ex) when (PostgresErrors.IsForeignKeyViolation(ex, "fk_emission_records_shipment"))
        {
            throw new NotFoundException(ShipmentNotFoundMessage);
        }
    }

    public async Task<EmissionRecord?> GetByIdAsync(
        int recordId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT record_id, shipment_id, factor_id, jarak_km, total_co2e_kg, metode, calculated_at
            FROM emission_records
            WHERE record_id = @RecordId
            """;

        var row = await RunAsync(
            transaction,
            conn => conn.QuerySingleOrDefaultAsync<EmissionRecordRow>(Command(
                sql, new { RecordId = recordId }, transaction, cancellationToken)),
            cancellationToken).ConfigureAwait(false);

        return row?.ToModel();
    }

    /// <summary>Bentuk mentah baris <c>emission_records</c>; <c>metode</c> masih teks.</summary>
    private sealed class EmissionRecordRow
    {
        public int RecordId { get; init; }

        public int ShipmentId { get; init; }

        public int FactorId { get; init; }

        public decimal JarakKm { get; init; }

        public decimal TotalCo2eKg { get; init; }

        public string Metode { get; init; } = string.Empty;

        public DateTimeOffset CalculatedAt { get; init; }

        public EmissionRecord ToModel() => new()
        {
            RecordId = RecordId,
            ShipmentId = ShipmentId,
            FactorId = FactorId,
            JarakKm = JarakKm,
            TotalCo2eKg = TotalCo2eKg,
            Metode = EmissionMethodExtensions.ParseEmissionMethod(Metode),
            CalculatedAt = CalculatedAt,
        };
    }
}
