using System.Net;
using Dapper;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Rules;
using KeranjangHijau.Core.Services;
using KeranjangHijau.Infrastructure;
using KeranjangHijau.Infrastructure.Data;
using KeranjangHijau.Infrastructure.Rendering;
using KeranjangHijau.Infrastructure.Security;
using KeranjangHijau.Infrastructure.Tracking;
using KeranjangHijau.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeranjangHijau.Tests.Configuration;

public class DependencyInjectionTests
{
    [Fact]
    public void ServiceProvider_BuildsAndResolvesCoreComponents()
    {
        var services = new ServiceCollection().AddKeranjangHijauBackend(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.IsType<SystemClock>(provider.GetRequiredService<IClock>());
        Assert.IsType<NpgsqlConnectionFactory>(provider.GetRequiredService<IDbConnectionFactory>());
        Assert.NotNull(provider.GetRequiredService<IHttpClientFactory>());
        Assert.True(DefaultTypeMap.MatchNamesWithUnderscores);
    }

    [Fact]
    public void ServiceProvider_ResolvesUserAndForestComponents()
    {
        using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(new ConfigurationBuilder().Build())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.IsType<UserRepository>(provider.GetRequiredService<IUserRepository>());
        Assert.IsType<ForestRepository>(provider.GetRequiredService<IForestRepository>());
        Assert.IsType<BcryptPasswordHasher>(provider.GetRequiredService<IPasswordHasher>());
        Assert.IsType<DbTransactionRunner>(provider.GetRequiredService<ITransactionRunner>());
        Assert.IsType<AuthService>(provider.GetRequiredService<IAuthService>());
        Assert.Same(provider.GetRequiredService<ICurrentUserSession>(), provider.GetRequiredService<ICurrentUserSession>());
    }

    [Fact]
    public void ServiceProvider_ResolvesResiComponents()
    {
        using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(new ConfigurationBuilder().Build())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.IsType<CourierRepository>(provider.GetRequiredService<ICourierRepository>());
        Assert.IsType<ShipmentRepository>(provider.GetRequiredService<IShipmentRepository>());
        Assert.IsType<ResiValidator>(provider.GetRequiredService<IResiValidator>());
    }

    [Fact]
    public void ServiceProvider_ResolvesTrackerAndCityComponents()
    {
        using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(new ConfigurationBuilder().Build())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.IsType<CityRepository>(provider.GetRequiredService<ICityRepository>());
        Assert.Same(provider.GetRequiredService<CompositeCourierTracker>(), provider.GetRequiredService<ICourierTracker>());
        Assert.IsType<RajaOngkirCourierApiAdapter>(provider.GetRequiredService<ICourierApiAdapter>());
    }

    [Theory]
    [InlineData("Generic", typeof(GenericCourierApiAdapter))]
    [InlineData(" generic ", typeof(GenericCourierApiAdapter))]
    [InlineData("RajaOngkir", typeof(RajaOngkirCourierApiAdapter))]
    [InlineData("rajaongkir", typeof(RajaOngkirCourierApiAdapter))]
    public void CourierApiProvider_SelectsAdapter(string provider, Type expected)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CourierApi:Provider"] = provider })
            .Build();
        using var sp = new ServiceCollection().AddKeranjangHijauBackend(config).BuildServiceProvider();

        Assert.IsType(expected, sp.GetRequiredService<ICourierApiAdapter>());
    }

    [Fact]
    public void UnknownCourierApiProvider_FailsValidation()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CourierApi:Provider"] = "ApiLain" })
            .Build();
        using var sp = new ServiceCollection().AddKeranjangHijauBackend(config).BuildServiceProvider();

        var ex = Assert.Throws<Microsoft.Extensions.Options.OptionsValidationException>(
            () => sp.GetRequiredService<ICourierApiAdapter>());
        Assert.Contains("CourierApi:Provider", ex.Message);
    }

    [Fact]
    public async Task CourierTracker_FromDi_UnsupportedCourier_UsesMockWithoutHttp()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CourierApi:BaseUrl"] = "https://rajaongkir.test/api/v1",
                ["CourierApi:ApiKey"] = "test-key",
            })
            .Build();
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, "{}");
        var services = new ServiceCollection().AddKeranjangHijauBackend(config);
        services.AddHttpClient(ApiCourierTracker.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();

        var result = await provider.GetRequiredService<ICourierTracker>().TrackAsync("sicepat", "004710030002");

        Assert.Empty(handler.Requests);
        Assert.NotNull(result);
        Assert.Equal(TrackingSource.Mock, result.Sumber);
    }

    [Fact]
    public void ServiceProvider_ResolvesDistanceAndModeRules_WithConfiguredOptions()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Emission:MinimumDistanceKm"] = "15",
                ["Emission:ExpressKeywords:0"] = "PRIORITAS",
            })
            .Build();

        using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(config)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var distance = provider.GetRequiredService<IDistanceCalculator>();
        var resolver = provider.GetRequiredService<ITransportModeResolver>();
        var city = new KeranjangHijau.Core.Models.City { CityId = 1, Latitude = -6.1754m, Longitude = 106.8272m };

        Assert.IsType<HaversineDistanceCalculator>(distance);
        Assert.IsType<TransportModeResolver>(resolver);
        Assert.Equal(15m, distance.CalculateDistanceKm(city, city));
        Assert.Equal(TransportMode.Pesawat, resolver.Resolve("Jawa", "Bali", "Prioritas"));
        Assert.Equal(TransportMode.Kapal, resolver.Resolve("Jawa", "Bali", "YES"));
    }

    [Fact]
    public void ServiceProvider_ResolvesEmissionCalculatorComponents()
    {
        using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(new ConfigurationBuilder().Build())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.IsType<EmissionFactorRepository>(provider.GetRequiredService<IEmissionFactorRepository>());
        Assert.IsType<WeightResolver>(provider.GetRequiredService<IWeightResolver>());
        Assert.IsType<LocalFormulaCalculator>(provider.GetRequiredService<ICarbonCalculator>());
    }

    [Fact]
    public void ServiceProvider_ResolvesEmissionService()
    {
        using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(new ConfigurationBuilder().Build())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.IsType<EmissionRecordRepository>(provider.GetRequiredService<IEmissionRecordRepository>());
        Assert.IsType<EmissionService>(provider.GetRequiredService<IEmissionService>());
        // Urutan penting: konversi setara (M4.1) lalu hutan (M5.1).
        Assert.Collection(
            provider.GetServices<IEmissionRecordedHandler>(),
            h => Assert.IsType<EquivalenceRecordedHandler>(h),
            h => Assert.IsType<ForestRecordedHandler>(h));
    }

    [Fact]
    public void ServiceProvider_ResolvesForestComponents()
    {
        using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(new ConfigurationBuilder().Build())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.IsType<ForestLogRepository>(provider.GetRequiredService<IForestLogRepository>());
        Assert.IsType<ForestService>(provider.GetRequiredService<IForestService>());
    }

    [Fact]
    public void ServiceProvider_ResolvesEquivalenceComponents()
    {
        using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(new ConfigurationBuilder().Build())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.IsType<EquivalenceRepository>(provider.GetRequiredService<IEquivalenceRepository>());
        Assert.IsType<EquivalenceCalculator>(provider.GetRequiredService<IEquivalenceCalculator>());
    }

    [Fact]
    public void ServiceProvider_ResolvesReportComponents()
    {
        using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(new ConfigurationBuilder().Build())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.IsType<ReportRepository>(provider.GetRequiredService<IReportRepository>());
        Assert.IsType<ReportService>(provider.GetRequiredService<IReportService>());
    }

    [Fact]
    public void ServiceProvider_ResolvesAdminComponents()
    {
        using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(new ConfigurationBuilder().Build())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.IsType<AdminQueryRepository>(provider.GetRequiredService<IAdminQueryRepository>());
        Assert.IsType<AdminService>(provider.GetRequiredService<IAdminService>());
    }

    [Fact]
    public async Task ServiceProvider_ResolvesShareComponents_WithPlaceholderRenderer()
    {
        using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(new ConfigurationBuilder().Build())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.IsType<SharedReportRepository>(provider.GetRequiredService<ISharedReportRepository>());
        Assert.IsType<ShareService>(provider.GetRequiredService<IShareService>());
        var renderer = Assert.IsType<NotConfiguredShareCardRenderer>(provider.GetRequiredService<IShareCardRenderer>());
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => renderer.RenderAsync(null!, "x.png"));
        Assert.Equal(NotConfiguredShareCardRenderer.NotConfiguredMessage, ex.Message);
    }

    [Fact]
    public void ShareCardRenderer_RegisteredBeforeOrAfterBackend_IsKept()
    {
        var renderer = new FakeShareCardRenderer();

        using var before = new ServiceCollection()
            .AddSingleton<IShareCardRenderer>(renderer)
            .AddKeranjangHijauBackend(new ConfigurationBuilder().Build())
            .BuildServiceProvider();
        using var after = new ServiceCollection()
            .AddKeranjangHijauBackend(new ConfigurationBuilder().Build())
            .AddSingleton<IShareCardRenderer>(renderer)
            .BuildServiceProvider();

        Assert.Same(renderer, before.GetRequiredService<IShareCardRenderer>());
        Assert.Same(renderer, after.GetRequiredService<IShareCardRenderer>());
    }

    [Fact]
    public void WeightResolver_FromDi_UsesConfiguredWeights()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Emission:DefaultWeightKg"] = "2",
                ["Emission:MaxWeightKg"] = "50",
            })
            .Build();

        using var provider = new ServiceCollection().AddKeranjangHijauBackend(config).BuildServiceProvider();
        var resolver = provider.GetRequiredService<IWeightResolver>();

        Assert.Equal(new KeranjangHijau.Core.Dtos.ResolvedWeight(2m, WeightSource.Default), resolver.Resolve(null, null));
        Assert.Throws<KeranjangHijau.Core.Exceptions.ValidationException>(() => resolver.Resolve(null, 60m));
    }

    [Fact]
    public void TransportModeResolver_FromDi_UsesDefaultExpressKeywords()
    {
        using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(new ConfigurationBuilder().Build())
            .BuildServiceProvider();

        var resolver = provider.GetRequiredService<ITransportModeResolver>();

        Assert.Equal(TransportMode.Pesawat, resolver.Resolve("Jawa", "Sumatra", "YES"));
        Assert.Equal(TransportMode.Kapal, resolver.Resolve("Jawa", "Sumatra", "REG"));
    }

    [Fact]
    public async Task CourierTracker_FromDi_FallsBackToMock_WhenApiFails()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CourierApi:BaseUrl"] = "https://api-cek-resi.test",
                ["CourierApi:ApiKey"] = "kunci-test",
            })
            .Build();
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable);
        var services = new ServiceCollection().AddKeranjangHijauBackend(config);
        services.AddHttpClient(ApiCourierTracker.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();

        var result = await provider.GetRequiredService<ICourierTracker>().TrackAsync("jne", "JNE1001000001");

        Assert.Single(handler.Requests);
        Assert.NotNull(result);
        Assert.Equal(TrackingSource.Mock, result.Sumber);
    }

    [Fact]
    public void SystemClock_ReturnsUtc()
    {
        Assert.Equal(TimeSpan.Zero, new SystemClock().UtcNow.Offset);
    }

    [Fact]
    public async Task OpenConnection_WithoutConnectionString_ThrowsHelpfulMessage()
    {
        await using var provider = new ServiceCollection()
            .AddKeranjangHijauBackend(new ConfigurationBuilder().Build())
            .BuildServiceProvider();
        var factory = provider.GetRequiredService<IDbConnectionFactory>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => factory.OpenConnectionAsync());

        Assert.Contains("KH_DB", ex.Message);
    }
}
