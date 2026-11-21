using KeranjangHijau.Infrastructure.Data;

namespace KeranjangHijau.Tests.Infrastructure;

/// <summary>Pola ILIKE kata kunci admin (tanpa database).</summary>
public class AdminQueryRepositoryPatternTests
{
    [Theory]
    [InlineData("siti", "%siti%")]
    [InlineData("100%", @"%100\%%")]
    [InlineData("a_b", @"%a\_b%")]
    [InlineData(@"a\b", @"%a\\b%")]
    [InlineData(@"\%_", @"%\\\%\_%")]
    public void ToContainsPattern_EscapesWildcards(string keyword, string expected)
    {
        Assert.Equal(expected, AdminQueryRepository.ToContainsPattern(keyword));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToContainsPattern_Empty_ReturnsNull(string? keyword)
    {
        Assert.Null(AdminQueryRepository.ToContainsPattern(keyword));
    }
}
