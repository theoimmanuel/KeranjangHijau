using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Services;

namespace KeranjangHijau.Tests.Services;

public class InMemoryCurrentUserSessionTests
{
    private static readonly UserDto Pengguna = new(1, "Budi", "budi@contoh.id", UserRole.Pengguna, DateTimeOffset.UnixEpoch);
    private static readonly UserDto Admin = new(2, "Admin", "admin@contoh.id", UserRole.Admin, DateTimeOffset.UnixEpoch);

    [Fact]
    public void NewSession_IsNotLoggedIn()
    {
        var session = new InMemoryCurrentUserSession();

        Assert.Null(session.CurrentUser);
        Assert.False(session.IsLoggedIn);
        Assert.False(session.IsAdmin);
    }

    [Fact]
    public void SignIn_Pengguna_IsLoggedInButNotAdmin()
    {
        var session = new InMemoryCurrentUserSession();

        session.SignIn(Pengguna);

        Assert.Same(Pengguna, session.CurrentUser);
        Assert.True(session.IsLoggedIn);
        Assert.False(session.IsAdmin);
    }

    [Fact]
    public void SignIn_Admin_IsAdmin()
    {
        var session = new InMemoryCurrentUserSession();

        session.SignIn(Admin);

        Assert.True(session.IsAdmin);
    }

    [Fact]
    public void SignIn_ReplacesPreviousUser_AndSignOutClears()
    {
        var session = new InMemoryCurrentUserSession();
        session.SignIn(Admin);

        session.SignIn(Pengguna);
        Assert.Same(Pengguna, session.CurrentUser);

        session.SignOut();
        Assert.False(session.IsLoggedIn);
    }

    [Fact]
    public void SignIn_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new InMemoryCurrentUserSession().SignIn(null!));
    }
}
