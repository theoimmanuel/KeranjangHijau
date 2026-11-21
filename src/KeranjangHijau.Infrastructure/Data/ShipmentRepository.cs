using System.Data;
using Dapper;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;
using Npgsql;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>Implementasi <see cref="IShipmentRepository"/> dengan Dapper (selalu query berparameter).</summary>
public sealed class ShipmentRepository : DapperRepositoryBase, IShipmentRepository
{
    public const string UserNotFoundMessage = "Pengguna tidak ditemukan.";

    private readonly IClock _clock;

    public ShipmentRepository(IDbConnectionFactory connectionFactory, IClock clock)
        : base(connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    public Task<bool> ExistsAsync(
        int courierId, string nomorResi, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nomorResi);

        // Memakai index idx_shipments_courier_resi (atau uq_shipments_courier_resi setelah rilis).
        return RunAsync(
            transaction,
            conn => conn.ExecuteScalarAsync<bool>(Command(
                "SELECT EXISTS (SELECT 1 FROM shipments WHERE courier_id = @CourierId AND nomor_resi = @NomorResi)",
                new { CourierId = courierId, NomorResi = ResiNormalizer.Normalize(nomorResi) },
                transaction,
                cancellationToken)),
            cancellationToken);
    }

    public async Task<int> InsertAsync(
        Shipment shipment, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentException.ThrowIfNullOrWhiteSpace(shipment.NomorResi, nameof(shipment.NomorResi));

        const string sql = """
            INSERT INTO shipments
                (user_id, courier_id, nomor_resi, origin_city_id, dest_city_id, layanan, berat_kg, sumber_data, created_at)
            VALUES
                (@UserId, @CourierId, @NomorResi, @OriginCityId, @DestCityId, @Layanan, @BeratKg, @SumberData, @CreatedAt)
            RETURNING shipment_id
            """;

        var nomorResi = ResiNormalizer.Normalize(shipment.NomorResi);
        var parameters = new
        {
            shipment.UserId,
            shipment.CourierId,
            NomorResi = nomorResi,
            shipment.OriginCityId,
            shipment.DestCityId,
            Layanan = string.IsNullOrWhiteSpace(shipment.Layanan) ? null : shipment.Layanan.Trim(),
            shipment.BeratKg,
            SumberData = shipment.SumberData.ToDbValue(),
            CreatedAt = (shipment.CreatedAt == default ? _clock.UtcNow : shipment.CreatedAt).ToUniversalTime(),
        };

        try
        {
            return await RunAsync(
                transaction,
                conn => conn.ExecuteScalarAsync<int>(Command(sql, parameters, transaction, cancellationToken)),
                cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException ex) when (PostgresErrors.IsUniqueViolation(ex, "uq_shipments_courier_resi"))
        {
            // Kode kurir tidak diketahui di sini (hanya courier_id); pesan ke pengguna tetap sama.
            throw new DuplicateResiException(string.Empty, nomorResi);
        }
        catch (PostgresException ex) when (PostgresErrors.IsForeignKeyViolation(ex, "fk_shipments_user"))
        {
            throw new NotFoundException(UserNotFoundMessage);
        }
    }
}
