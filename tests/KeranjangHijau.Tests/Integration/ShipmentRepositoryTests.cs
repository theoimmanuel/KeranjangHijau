using Dapper;

namespace KeranjangHijau.Tests.Integration;

public class ShipmentRepositoryTests : RepositoryTestBase
{
    [DatabaseFact]
    public async Task Exists_ReturnsTrueOnlyForSameCourierAndResi()
    {
        var jne = await Couriers.GetByKodeAsync("jne", Transaction);
        var jnt = await Couriers.GetByKodeAsync("jnt", Transaction);
        Assert.NotNull(jne);
        Assert.NotNull(jnt);

        var resi = $"KH{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        Assert.False(await Shipments.ExistsAsync(jne.CourierId, resi, Transaction));

        await InsertShipmentAsync(jne.CourierId, resi);

        Assert.True(await Shipments.ExistsAsync(jne.CourierId, resi, Transaction));
        Assert.True(await Shipments.ExistsAsync(jne.CourierId, $" {resi.ToLowerInvariant()} ", Transaction));
        Assert.False(await Shipments.ExistsAsync(jnt.CourierId, resi, Transaction));
    }

    private async Task InsertShipmentAsync(int courierId, string resi)
    {
        var userId = await Users.InsertAsync(NewUser(), Transaction);
        await Transaction.Connection!.ExecuteAsync(
            """
            INSERT INTO shipments (user_id, courier_id, nomor_resi, origin_city_id, dest_city_id, layanan, berat_kg, sumber_data)
            SELECT @UserId, @CourierId, @Resi, c.city_id, c.city_id, 'REG', 1, 'manual'
            FROM cities c
            ORDER BY c.city_id
            LIMIT 1
            """,
            new { UserId = userId, CourierId = courierId, Resi = resi },
            Transaction);
    }
}
