namespace KeranjangHijau.Tests.Integration;

public class CourierRepositoryTests : RepositoryTestBase
{
    [DatabaseFact]
    public async Task GetAll_ReturnsSeededCouriers()
    {
        var couriers = await Couriers.GetAllAsync(Transaction);

        Assert.Contains(couriers, c => c.KodeKurir == "jne" && c.NamaKurir == "JNE");
        Assert.Equal(
            ["anteraja", "jne", "jnt", "pos", "sicepat"],
            couriers.Select(c => c.KodeKurir).Intersect(["jne", "jnt", "sicepat", "anteraja", "pos"]).Order());
        Assert.All(couriers, c => Assert.True(c.CourierId > 0));
    }

    [DatabaseFact]
    public async Task GetByKode_IgnoresCaseAndSpaces()
    {
        var courier = await Couriers.GetByKodeAsync("  SiCepat ", Transaction);

        Assert.NotNull(courier);
        Assert.Equal("sicepat", courier.KodeKurir);
        Assert.Equal("SiCepat Ekspres", courier.NamaKurir);
    }

    [DatabaseFact]
    public async Task GetByKode_Unknown_ReturnsNull()
    {
        Assert.Null(await Couriers.GetByKodeAsync("tidak-ada", Transaction));
    }
}
