using KeranjangHijau.Core.Options;
using KeranjangHijau.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Tests.Configuration;

public class ConfigurationTests
{
    [Fact]
    public void ShippedAppSettings_BindsDocumentedDefaults()
    {
        using var provider = BuildProvider(ShippedAppSettingsOnly());

        var emission = provider.GetRequiredService<IOptions<EmissionOptions>>().Value;
        Assert.Equal(1.3m, emission.RouteCorrectionFactor);
        Assert.Equal(10m, emission.MinimumDistanceKm);
        Assert.Equal(1m, emission.DefaultWeightKg);
        Assert.Equal(100m, emission.MaxWeightKg);
        // Tepat 8: memastikan binder tidak menggandakan daftar kata kunci.
        Assert.Equal(EmissionOptions.DefaultExpressKeywords, emission.ExpressKeywords);

        var forest = provider.GetRequiredService<IOptions<ForestOptions>>().Value;
        Assert.Equal(1m, forest.PointsPer100GramCo2e);
        Assert.Equal(1m, forest.RecoveryPointsPerDay);
        Assert.Equal(100m, forest.InitialHealth);
        Assert.Equal(100, forest.InitialTrees);

        Assert.True(provider.GetRequiredService<IOptions<FeatureOptions>>().Value.AllowDuplicateResi);
        var api = provider.GetRequiredService<IOptions<CourierApiOptions>>().Value;
        Assert.Equal(5, api.TimeoutSeconds);
        Assert.Equal(CourierApiOptions.RajaOngkirProvider, api.Provider);
        Assert.Equal("https://rajaongkir.komerce.id/api/v1", api.BaseUrl);
        // Tepat 11: memastikan binder tidak menggandakan daftar kurir.
        Assert.Equal(CourierApiOptions.DefaultSupportedCouriers, api.SupportedCouriers);
    }

    [Fact]
    public void ShippedAppSettings_ContainsNoSecrets()
    {
        using var provider = BuildProvider(ShippedAppSettingsOnly());

        Assert.Empty(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString);
        var api = provider.GetRequiredService<IOptions<CourierApiOptions>>().Value;
        Assert.Empty(api.ApiKey);
        Assert.False(api.HasApiKey);
    }

    [Fact]
    public void EmptyConfiguration_FallsBackToCodeDefaults()
    {
        using var provider = BuildProvider(new ConfigurationBuilder().Build());

        var emission = provider.GetRequiredService<IOptions<EmissionOptions>>().Value;
        Assert.Equal(1.3m, emission.RouteCorrectionFactor);
        Assert.Equal(EmissionOptions.DefaultExpressKeywords, emission.ExpressKeywords);
        Assert.True(provider.GetRequiredService<IOptions<FeatureOptions>>().Value.AllowDuplicateResi);
        Assert.Equal(100, provider.GetRequiredService<IOptions<ForestOptions>>().Value.InitialTrees);

        var api = provider.GetRequiredService<IOptions<CourierApiOptions>>().Value;
        Assert.Equal(CourierApiOptions.RajaOngkirProvider, api.Provider);
        Assert.Equal(CourierApiOptions.DefaultSupportedCouriers, api.SupportedCouriers);
    }

    [Fact]
    public void SupportedCouriers_FromConfiguration_ReplaceDefaults()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([
                new KeyValuePair<string, string?>("CourierApi:SupportedCouriers:0", "jne"),
                new KeyValuePair<string, string?>("CourierApi:SupportedCouriers:1", "pos"),
            ])
            .Build();
        using var provider = BuildProvider(config);

        var api = provider.GetRequiredService<IOptions<CourierApiOptions>>().Value;

        Assert.Equal(["jne", "pos"], api.SupportedCouriers);
        Assert.True(api.SupportsCourier(" JNE "));
        Assert.False(api.SupportsCourier("jnt"));
    }

    [Fact]
    public void ShippedFiles_ContainNoRealApiKey()
    {
        // appsettings*.json dan .env.example hanya boleh berisi API key kosong/placeholder.
        var api = new ConfigurationBuilder()
            .AddJsonFile(RepoFiles.Path("src", "KeranjangHijau.Infrastructure", "appsettings.Local.example.json"), optional: false)
            .Build()["CourierApi:ApiKey"];
        Assert.True(string.IsNullOrEmpty(api));

        var example = File.ReadAllLines(RepoFiles.Path(".env.example"))
            .Single(l => l.StartsWith("KH_CourierApi__ApiKey=", StringComparison.Ordinal));
        Assert.Equal("KH_CourierApi__ApiKey=ISI_API_KEY_RAJAONGKIR", example);
    }

    [Fact]
    public void LocalSettings_OverrideAppSettings_AndKhDbOverridesBoth()
    {
        using var dir = new TempDirectory();
        dir.Write("appsettings.json", """{ "Features": { "AllowDuplicateResi": true }, "Database": { "ConnectionString": "" } }""");
        dir.Write("appsettings.Local.json", """{ "Features": { "AllowDuplicateResi": false }, "Database": { "ConnectionString": "Host=dari-local" } }""");

        var withoutKhDb = new ConfigurationBuilder().AddKeranjangHijauSources(dir.Path, khDb: null).Build();
        var withKhDb = new ConfigurationBuilder().AddKeranjangHijauSources(dir.Path, khDb: "Host=dari-env").Build();

        Assert.Equal("False", withoutKhDb["Features:AllowDuplicateResi"]);
        Assert.Equal("Host=dari-local", withoutKhDb["Database:ConnectionString"]);
        Assert.Equal("Host=dari-env", withKhDb["Database:ConnectionString"]);
    }

    [Fact]
    public void MissingLocalSettings_IsOptional()
    {
        using var dir = new TempDirectory();
        dir.Write("appsettings.json", """{ "Features": { "AllowDuplicateResi": true } }""");

        var config = new ConfigurationBuilder().AddKeranjangHijauSources(dir.Path, khDb: null).Build();

        Assert.Equal("True", config["Features:AllowDuplicateResi"]);
    }

    [Theory]
    [InlineData("Emission:RouteCorrectionFactor", "0.5", "RouteCorrectionFactor")]
    [InlineData("Emission:DefaultWeightKg", "150", "DefaultWeightKg")]
    public void InvalidEmissionOptions_FailWithIndonesianMessage(string key, string value, string expectedInMessage)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>(key, value)])
            .Build();
        using var provider = BuildProvider(config);

        var ex = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<EmissionOptions>>().Value);

        Assert.Contains(expectedInMessage, ex.Message);
    }

    [Fact]
    public void InvalidForestOptions_FailValidation()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("Forest:InitialHealth", "120")])
            .Build();
        using var provider = BuildProvider(config);

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<ForestOptions>>().Value);
    }

    private static IConfiguration ShippedAppSettingsOnly() =>
        new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, BackendConfiguration.AppSettingsFile), optional: false)
            .Build();

    private static ServiceProvider BuildProvider(IConfiguration configuration) =>
        new ServiceCollection().AddKeranjangHijauBackend(configuration).BuildServiceProvider();

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kh-config-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Write(string fileName, string content) =>
            File.WriteAllText(System.IO.Path.Combine(Path, fileName), content);

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
