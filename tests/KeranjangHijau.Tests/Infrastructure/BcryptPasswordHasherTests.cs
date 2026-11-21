using KeranjangHijau.Infrastructure.Security;

namespace KeranjangHijau.Tests.Infrastructure;

public class BcryptPasswordHasherTests
{
    // Cost kecil supaya test cepat; algoritmanya tetap sama.
    private readonly BcryptPasswordHasher _hasher = new(workFactor: 4);

    [Fact]
    public void Hash_ThenVerify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("RahasiaHijau123!");

        Assert.True(_hasher.Verify("RahasiaHijau123!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("RahasiaHijau123!");

        Assert.False(_hasher.Verify("rahasiahijau123!", hash));
    }

    [Fact]
    public void Hash_DoesNotContainPlainPassword_AndIsBcryptFormat()
    {
        var hash = _hasher.Hash("RahasiaHijau123!");

        Assert.DoesNotContain("RahasiaHijau123!", hash);
        Assert.Matches(@"^\$2[aby]\$04\$.{53}$", hash);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        Assert.NotEqual(_hasher.Hash("sama-saja"), _hasher.Hash("sama-saja"));
    }

    [Fact]
    public void DefaultConstructor_UsesWorkFactor11()
    {
        var hash = new BcryptPasswordHasher().Hash("x");

        Assert.StartsWith("$2a$11$", hash);
    }

    [Fact]
    public void Verify_SeedAdminHash_MatchesReadmePassword()
    {
        // Hash akun admin di database/seed.sql dan password di README "Akun demo".
        const string seedHash = "$2a$11$QBArkqLcwm2TI4WcIAFhyuKAJYKI.OEVCqiu1boSBfDCk3MxalQYu";

        Assert.True(_hasher.Verify("AdminKH2026!", seedHash));
        Assert.False(_hasher.Verify("adminkh2026!", seedHash));
    }

    [Theory]
    [InlineData("bukan-hash-bcrypt")]
    [InlineData("$2a$11$terlalupendek")]
    [InlineData("")]
    [InlineData("   ")]
    public void Verify_InvalidHash_ReturnsFalse(string invalidHash)
    {
        Assert.False(_hasher.Verify("password", invalidHash));
    }

    [Fact]
    public void Verify_EmptyPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("password");

        Assert.False(_hasher.Verify(string.Empty, hash));
    }

    [Fact]
    public void Hash_EmptyPassword_Throws()
    {
        Assert.Throws<ArgumentException>(() => _hasher.Hash(string.Empty));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(32)]
    public void Constructor_WorkFactorOutOfRange_Throws(int workFactor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BcryptPasswordHasher(workFactor));
    }
}
