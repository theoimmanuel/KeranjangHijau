using KeranjangHijau.Core.Enums;

namespace KeranjangHijau.Tests.Core;

public class UserRoleTests
{
    [Theory]
    [InlineData(UserRole.Pengguna, "pengguna")]
    [InlineData(UserRole.Admin, "admin")]
    public void ToDbValue_ReturnsLowercaseText(UserRole role, string expected)
    {
        Assert.Equal(expected, role.ToDbValue());
    }

    [Theory]
    [InlineData("pengguna", UserRole.Pengguna)]
    [InlineData("admin", UserRole.Admin)]
    [InlineData("ADMIN", UserRole.Admin)]
    [InlineData("  Pengguna ", UserRole.Pengguna)]
    public void ParseUserRole_AcceptsDbText(string text, UserRole expected)
    {
        Assert.Equal(expected, UserRoleExtensions.ParseUserRole(text));
    }

    [Theory]
    [InlineData(UserRole.Pengguna)]
    [InlineData(UserRole.Admin)]
    public void RoundTrip_ReturnsSameRole(UserRole role)
    {
        Assert.Equal(role, UserRoleExtensions.ParseUserRole(role.ToDbValue()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("superadmin")]
    [InlineData("0")]
    public void ParseUserRole_UnknownText_Throws(string text)
    {
        Assert.Throws<ArgumentException>(() => UserRoleExtensions.ParseUserRole(text));
    }

    [Fact]
    public void TryParseUserRole_Null_ReturnsFalse()
    {
        Assert.False(UserRoleExtensions.TryParseUserRole(null, out _));
    }

    [Fact]
    public void ToDbValue_UndefinedEnumValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((UserRole)99).ToDbValue());
    }

    [Fact]
    public void DbValues_MatchSchemaCheckConstraint()
    {
        // ck_users_role CHECK (role IN ('pengguna', 'admin'))
        var values = Enum.GetValues<UserRole>().Select(r => r.ToDbValue()).Order();

        Assert.Equal(["admin", "pengguna"], values);
    }
}
