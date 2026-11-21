using System.Globalization;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Rules;

public class ForestTreeRuleTests
{
    [Theory]
    [InlineData(100, "100", 100)]
    [InlineData(100, "0", 0)]
    [InlineData(100, "57.25", 57)]
    [InlineData(100, "57.5", 58)]   // AwayFromZero
    [InlineData(150, "62.4", 94)]   // 93,6
    [InlineData(150, "1", 2)]       // 1,5 → 2
    [InlineData(100, "0.49", 0)]
    public void EstimateRemainingTrees_RoundsToNearest(int pohon, string kesehatan, int expected)
    {
        Assert.Equal(expected, ForestTreeRule.EstimateRemainingTrees(pohon, decimal.Parse(kesehatan, CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void EstimateRemainingTrees_InvalidInput_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ForestTreeRule.EstimateRemainingTrees(-1, 50m));
        Assert.Throws<ArgumentOutOfRangeException>(() => ForestTreeRule.EstimateRemainingTrees(100, 100.01m));
        Assert.Throws<ArgumentOutOfRangeException>(() => ForestTreeRule.EstimateRemainingTrees(100, -1m));
    }

    [Fact]
    public void ForestDto_FromModel_ComputesStatusAndTrees_FromHealth()
    {
        // Status di model sengaja usang: DTO menghitung ulang dari kesehatan.
        var forest = new Forest { NamaHutan = "Hutan Budi", JumlahPohonAwal = 100, Kesehatan = 19.99m, Status = "Lestari" };

        Assert.Equal(new ForestDto("Hutan Budi", 19.99m, ForestStatus.Kritis, 100, 20), ForestDto.FromModel(forest));
    }
}
