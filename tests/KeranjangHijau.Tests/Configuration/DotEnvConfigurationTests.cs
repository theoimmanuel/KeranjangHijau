using KeranjangHijau.Core.Options;
using KeranjangHijau.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Tests.Configuration;

/// <summary>
/// Pembaca <c>.env</c> (M2.4). Environment diberikan lewat dictionary (berisi <c>KH_ENV_FILE</c>),
/// jadi environment proses tidak pernah diubah dan <c>.env</c> milik pengembang tidak ikut terbaca.
/// </summary>
public sealed class DotEnvConfigurationTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "kh-dotenv-" + Guid.NewGuid().ToString("N"));

    public DotEnvConfigurationTests()
    {
        Directory.CreateDirectory(_dir);
        Write("appsettings.json", """
            { "Database": { "ConnectionString": "" },
              "Features": { "AllowDuplicateResi": true },
              "CourierApi": { "ApiKey": "", "BaseUrl": "https://dari-appsettings.test" } }
            """);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Write(string fileName, string content)
    {
        var path = Path.Combine(_dir, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    private IConfigurationRoot Build(Dictionary<string, string?> environment, params string?[] searchDirectories) =>
        new ConfigurationBuilder()
            .AddKeranjangHijauSources(_dir, environment, searchDirectories)
            .Build();

    private Dictionary<string, string?> EnvWithFile(string path) => new() { [BackendConfiguration.EnvFileVariable] = path };

    [Fact]
    public void DotEnv_ParsesCommentsBlankLinesQuotesAndIgnoresNonKhLines()
    {
        var path = Write("test.env", """
            # komentar
               # komentar dengan spasi di depan

            POSTGRES_PORT=5433
            KH_CourierApi__ApiKey = "test-key"
            KH_CourierApi__BaseUrl='https://dari-env-file.test'
              KH_Features__AllowDuplicateResi=false
            KH_Emission__RouteCorrectionFactor=1.5
            baris tanpa tanda sama dengan
            =nilai tanpa kunci
            """);

        var config = Build(EnvWithFile(path));

        Assert.Equal("test-key", config["CourierApi:ApiKey"]);
        Assert.Equal("https://dari-env-file.test", config["CourierApi:BaseUrl"]);
        Assert.Equal("false", config["Features:AllowDuplicateResi"]);
        Assert.Equal("1.5", config["Emission:RouteCorrectionFactor"]);
        Assert.Null(config["POSTGRES_PORT"]);
        Assert.Null(config["PORT"]);
    }

    [Fact]
    public void DotEnv_ValueMayContainEqualsAndHash()
    {
        var path = Write("test.env", "KH_Database__ConnectionString=Host=x;Password=a=b#c\n");

        Assert.Equal("Host=x;Password=a=b#c", Build(EnvWithFile(path))["Database:ConnectionString"]);
    }

    [Fact]
    public void DotEnv_KhDb_MapsToConnectionString()
    {
        var path = Write("test.env", "KH_DB=postgresql://u:p@localhost:5432/db\n");

        Assert.Equal("postgresql://u:p@localhost:5432/db", Build(EnvWithFile(path))["Database:ConnectionString"]);
    }

    [Fact]
    public void DotEnv_KhDb_BeatsConnectionStringKeyInSameFile()
    {
        var path = Write("test.env", """
            KH_DB=Host=dari-kh-db
            KH_Database__ConnectionString=Host=dari-kunci-biasa
            """);

        Assert.Equal("Host=dari-kh-db", Build(EnvWithFile(path))["Database:ConnectionString"]);
    }

    [Fact]
    public void DotEnv_OverridesAppSettingsAndLocalSettings()
    {
        Write("appsettings.Local.json", """{ "CourierApi": { "ApiKey": "dari-local" } }""");
        var path = Write("test.env", "KH_CourierApi__ApiKey=dari-env-file\n");

        Assert.Equal("dari-env-file", Build(EnvWithFile(path))["CourierApi:ApiKey"]);
    }

    [Fact]
    public void RealEnvironment_WinsOverDotEnv()
    {
        var path = Write("test.env", """
            KH_CourierApi__ApiKey=dari-env-file
            KH_DB=Host=dari-env-file
            KH_Features__AllowDuplicateResi=false
            """);
        var environment = EnvWithFile(path);
        environment["KH_CourierApi__ApiKey"] = "dari-env-asli";
        environment["KH_Database__ConnectionString"] = "Host=dari-env-asli";

        var config = Build(environment);

        Assert.Equal("dari-env-asli", config["CourierApi:ApiKey"]);
        Assert.Equal("Host=dari-env-asli", config["Database:ConnectionString"]);
        Assert.Equal("false", config["Features:AllowDuplicateResi"]); // tidak ditimpa env asli
    }

    [Fact]
    public void RealKhDb_WinsOverEverything()
    {
        var path = Write("test.env", "KH_DB=Host=dari-env-file\n");
        var environment = EnvWithFile(path);
        environment["KH_DB"] = "Host=dari-kh-db-asli";
        environment["KH_Database__ConnectionString"] = "Host=dari-kunci-asli";

        Assert.Equal("Host=dari-kh-db-asli", Build(environment)["Database:ConnectionString"]);
    }

    [Fact]
    public void MissingFile_IsNotAnError()
    {
        var config = Build(EnvWithFile(Path.Combine(_dir, "tidak-ada.env")));

        Assert.Equal(string.Empty, config["CourierApi:ApiKey"]);
        Assert.Null(BackendConfiguration.ResolveDotEnvPath(EnvWithFile(Path.Combine(_dir, "tidak-ada.env")), []));
    }

    [Fact]
    public void NoEnvFileVariable_AndNothingFound_IsNotAnError()
    {
        var config = Build(new Dictionary<string, string?>());

        Assert.Equal("https://dari-appsettings.test", config["CourierApi:BaseUrl"]);
    }

    [Fact]
    public void KhEnvFile_IsNotMappedAsConfigurationKey()
    {
        var path = Write("test.env", "KH_ENV_FILE=lain.env\n");

        var config = Build(EnvWithFile(path));

        Assert.Null(config["ENV_FILE"]);
    }

    [Fact]
    public void DotEnv_FeedsOptionsThroughDependencyInjection()
    {
        var path = Write("test.env", "KH_CourierApi__ApiKey=test-key\n");
        using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(Build(EnvWithFile(path)))
            .BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<CourierApiOptions>>().Value;

        Assert.Equal("test-key", options.ApiKey);
        Assert.True(options.HasApiKey);
    }

    [Fact]
    public void Locate_WalksUpToParent_AndStopsAtRepoRoot()
    {
        var repo = Directory.CreateDirectory(Path.Combine(_dir, "repo"));
        var nested = Directory.CreateDirectory(Path.Combine(repo.FullName, "src", "App", "bin"));
        File.WriteAllText(Path.Combine(repo.FullName, "KeranjangHijau.sln"), string.Empty);

        // Belum ada .env di repo: pencarian berhenti di root repo, tidak naik ke _dir.
        Write(".env", "KH_CourierApi__ApiKey=di-luar-repo\n");
        Assert.Null(DotEnvFile.Locate([nested.FullName]));

        var repoEnv = Path.Combine(repo.FullName, ".env");
        File.WriteAllText(repoEnv, "KH_CourierApi__ApiKey=di-root-repo\n");
        Assert.Equal(repoEnv, DotEnvFile.Locate([nested.FullName]));
    }

    [Fact]
    public void Locate_StopsAtFolderWithGit_AndTriesNextStartDirectory()
    {
        var first = Directory.CreateDirectory(Path.Combine(_dir, "pertama"));
        Directory.CreateDirectory(Path.Combine(first.FullName, ".git"));
        var second = Directory.CreateDirectory(Path.Combine(_dir, "kedua"));
        File.WriteAllText(Path.Combine(second.FullName, "KeranjangHijau.sln"), string.Empty);
        var secondEnv = Path.Combine(second.FullName, ".env");
        File.WriteAllText(secondEnv, "KH_X=1\n");

        Assert.Equal(secondEnv, DotEnvFile.Locate([null, "  ", first.FullName, second.FullName]));
    }

    [Fact]
    public void LegacyOverloadWithKhDb_NeverReadsDotEnv()
    {
        // Overload (basePath, khDb) dipakai test lama: tidak boleh terpengaruh .env di folder mana pun.
        Write(".env", "KH_CourierApi__ApiKey=tidak-boleh-terbaca\n");

        var config = new ConfigurationBuilder().AddKeranjangHijauSources(_dir, khDb: "Host=param").Build();

        Assert.Equal(string.Empty, config["CourierApi:ApiKey"]);
        Assert.Equal("Host=param", config["Database:ConnectionString"]);
    }

    [Theory]
    [InlineData("KH_A__B__C", "A:B:C")]
    [InlineData("kh_Features__AllowDuplicateResi", "Features:AllowDuplicateResi")]
    public void MapKhVariables_StripsPrefixAndTurnsDoubleUnderscoreIntoColon(string key, string expected)
    {
        var mapped = BackendConfiguration.MapKhVariables([new(key, "nilai")]);

        Assert.Equal("nilai", Assert.Single(mapped, p => p.Key == expected).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MapKhVariables_EmptyKhDb_IsIgnored(string value)
    {
        Assert.Empty(BackendConfiguration.MapKhVariables([new("KH_DB", value), new("OTHER", "x"), new("KH_", "y")]));
    }
}
