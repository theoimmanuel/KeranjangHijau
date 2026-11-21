using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Tests.Core;

public class ShipmentDataSourceTests
{
    [Theory]
    [InlineData(ShipmentDataSource.Api, "api")]
    [InlineData(ShipmentDataSource.Mock, "mock")]
    [InlineData(ShipmentDataSource.Manual, "manual")]
    public void ToDbValue_ReturnsSchemaText(ShipmentDataSource source, string expected)
    {
        Assert.Equal(expected, source.ToDbValue());
        Assert.Equal(source, ShipmentDataSourceExtensions.ParseShipmentDataSource(expected.ToUpperInvariant()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("rumus_lokal")]
    public void Parse_UnknownText_Throws(string text)
    {
        Assert.Throws<ArgumentException>(() => ShipmentDataSourceExtensions.ParseShipmentDataSource(text));
    }

    [Fact]
    public void DbValues_MatchSchemaCheckConstraint()
    {
        // ck_shipments_sumber_data CHECK (sumber_data IN ('api', 'mock', 'manual'))
        var values = Enum.GetValues<ShipmentDataSource>().Select(s => s.ToDbValue()).Order();

        Assert.Equal(["api", "manual", "mock"], values);
    }

    [Theory]
    [InlineData(TrackingSource.Api, WeightSource.Api, ShipmentDataSource.Api)]
    [InlineData(TrackingSource.Api, WeightSource.Default, ShipmentDataSource.Api)]
    [InlineData(TrackingSource.Mock, WeightSource.Api, ShipmentDataSource.Mock)]
    [InlineData(TrackingSource.Mock, WeightSource.Default, ShipmentDataSource.Mock)]
    [InlineData(TrackingSource.Api, WeightSource.Manual, ShipmentDataSource.Manual)]
    [InlineData(TrackingSource.Mock, WeightSource.Manual, ShipmentDataSource.Manual)]
    public void From_ManualWeightWins_OtherwiseFollowsTracker(TrackingSource tracking, WeightSource weight, ShipmentDataSource expected)
    {
        Assert.Equal(expected, ShipmentDataSourceExtensions.From(tracking, weight));
    }
}
