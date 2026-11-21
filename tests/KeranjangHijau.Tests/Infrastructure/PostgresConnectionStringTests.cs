using KeranjangHijau.Infrastructure.Data;
using Npgsql;

namespace KeranjangHijau.Tests.Infrastructure;

public class PostgresConnectionStringTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_Throws(string? value)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => PostgresConnectionString.Normalize(value));
        Assert.Equal(PostgresConnectionString.MissingMessage, ex.Message);
    }

    [Fact]
    public void KeyValueFormat_IsKeptAsIs()
    {
        const string cs = "Host=localhost;Port=5433;Database=kh;Username=u;Password=p";

        Assert.Equal(cs, PostgresConnectionString.Normalize("  " + cs + " "));
    }

    [Theory]
    [InlineData("postgresql://kh:rahasia@localhost:5433/keranjanghijau")]
    [InlineData("postgres://kh:rahasia@localhost:5433/keranjanghijau")]
    public void UriFormat_IsConverted(string uri)
    {
        var b = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(uri));

        Assert.Equal("localhost", b.Host);
        Assert.Equal(5433, b.Port);
        Assert.Equal("keranjanghijau", b.Database);
        Assert.Equal("kh", b.Username);
        Assert.Equal("rahasia", b.Password);
    }

    [Fact]
    public void UriFormat_DecodesEscapedPassword_DefaultsPort_AndMapsSslMode()
    {
        var b = new NpgsqlConnectionStringBuilder(
            PostgresConnectionString.Normalize("postgresql://kh:p%40ss%3Aw0rd@db.example.com/postgres?sslmode=require"));

        Assert.Equal("p@ss:w0rd", b.Password);
        Assert.Equal(5432, b.Port);
        Assert.Equal(SslMode.Require, b.SslMode);
    }

    [Fact]
    public void UriFormat_UnknownParameter_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => PostgresConnectionString.Normalize("postgresql://kh@localhost/db?bukan_parameter=1"));
    }
}
