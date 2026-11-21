using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Tests.Core;

public class ForestStatusTests
{
    [Theory]
    [InlineData(ForestStatus.Lestari, "Lestari")]
    [InlineData(ForestStatus.Sehat, "Sehat")]
    [InlineData(ForestStatus.Menipis, "Menipis")]
    [InlineData(ForestStatus.Terancam, "Terancam")]
    [InlineData(ForestStatus.Kritis, "Kritis")]
    public void ToDbValue_MatchesCheckConstraintText(ForestStatus status, string expected)
    {
        Assert.Equal(expected, status.ToDbValue());
    }

    [Theory]
    [InlineData("Lestari", ForestStatus.Lestari)]
    [InlineData("kritis", ForestStatus.Kritis)]
    [InlineData("  TERANCAM ", ForestStatus.Terancam)]
    public void ParseForestStatus_AcceptsDbText(string text, ForestStatus expected)
    {
        Assert.Equal(expected, ForestStatusExtensions.ParseForestStatus(text));
    }

    [Fact]
    public void RoundTrip_AllStatuses()
    {
        foreach (var status in Enum.GetValues<ForestStatus>())
        {
            Assert.Equal(status, ForestStatusExtensions.ParseForestStatus(status.ToDbValue()));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("Gundul")]
    [InlineData("0")]
    public void ParseForestStatus_UnknownText_Throws(string text)
    {
        Assert.Throws<ArgumentException>(() => ForestStatusExtensions.ParseForestStatus(text));
        Assert.False(ForestStatusExtensions.TryParseForestStatus(text, out _));
    }

    [Fact]
    public void ToDbValue_UndefinedValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ForestStatus)99).ToDbValue());
    }

    [Fact]
    public void DbValues_MatchSchemaCheckConstraint()
    {
        var schema = File.ReadAllText(RepoFiles.Path("database", "schema.sql"));

        Assert.Contains("CHECK (status IN ('Lestari', 'Sehat', 'Menipis', 'Terancam', 'Kritis'))", schema);
    }
}
