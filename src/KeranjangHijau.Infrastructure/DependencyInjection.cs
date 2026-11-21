using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Core.Rules;
using KeranjangHijau.Core.Services;
using KeranjangHijau.Infrastructure.Data;
using KeranjangHijau.Infrastructure.Rendering;
using KeranjangHijau.Infrastructure.Security;
using KeranjangHijau.Infrastructure.Tracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Infrastructure;

/// <summary>
/// Satu pintu masuk untuk mendaftarkan seluruh backend ke container DI.
/// Pemakaian di proyek WPF:
/// <code>
/// var config = BackendConfiguration.Build();
/// services.AddKeranjangHijauBackend(config);
/// </code>
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddKeranjangHijauBackend(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        DapperConfiguration.Configure();

        AddOptions(services, configuration);

        // Komponen dasar
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddHttpClient();

        // Keamanan
        services.AddSingleton<IPasswordHasher>(_ => new BcryptPasswordHasher());

        // Repository (tanpa state; aman sebagai singleton)
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IForestRepository, ForestRepository>();
        services.AddSingleton<ICourierRepository, CourierRepository>();
        services.AddSingleton<IShipmentRepository, ShipmentRepository>();
        services.AddSingleton<ICityRepository, CityRepository>();

        // Transaksi lintas repository (registrasi, hitung emisi)
        services.AddSingleton<ITransactionRunner, DbTransactionRunner>();

        // Sesi login dan autentikasi
        services.AddSingleton<ICurrentUserSession, InMemoryCurrentUserSession>();
        services.AddSingleton<IAuthService, AuthService>();

        // Validasi resi (cek duplikat mengikuti Features:AllowDuplicateResi)
        services.AddSingleton<IResiValidator, ResiValidator>();

        // Tracker resi (CLAUDE.md 7.10): CompositeCourierTracker mencoba API lalu mock.
        // Named client (bukan typed client) karena tracker singleton; HttpClient diminta per panggilan.
        services.AddHttpClient(ApiCourierTracker.HttpClientName);
        // Adapter dipilih lewat CourierApi:Provider (M2.4): RajaOngkir (default) atau Generic (contoh M2.3).
        services.AddSingleton<RajaOngkirCourierApiAdapter>();
        services.AddSingleton<GenericCourierApiAdapter>();
        services.AddSingleton<ICourierApiAdapter>(sp =>
            sp.GetRequiredService<IOptions<CourierApiOptions>>().Value.UsesGenericProvider
                ? sp.GetRequiredService<GenericCourierApiAdapter>()
                : sp.GetRequiredService<RajaOngkirCourierApiAdapter>());
        services.AddSingleton<ApiCourierTracker>();
        services.AddSingleton(_ => new MockCourierTracker());
        services.AddSingleton<CompositeCourierTracker>();
        services.AddSingleton<ICourierTracker>(sp => sp.GetRequiredService<CompositeCourierTracker>());

        // Aturan perhitungan (CLAUDE.md 7.2–7.3): murni tanpa I/O, membaca EmissionOptions per panggilan.
        services.AddSingleton<IDistanceCalculator, HaversineDistanceCalculator>();
        services.AddSingleton<ITransportModeResolver, TransportModeResolver>();

        // Kalkulator emisi (CLAUDE.md 7.1, 7.4)
        services.AddSingleton<IEmissionFactorRepository, EmissionFactorRepository>();
        services.AddSingleton<IWeightResolver, WeightResolver>();
        services.AddSingleton<ICarbonCalculator, LocalFormulaCalculator>();

        // Alur hitung emisi (M3.3). Langkah lanjutan setelah record tersimpan (konversi setara M4.1,
        // hutan M5.1) didaftarkan sebagai IEmissionRecordedHandler; dipanggil sesuai urutan pendaftaran.
        services.AddSingleton<IEmissionRecordRepository, EmissionRecordRepository>();
        services.AddSingleton<IEmissionService, EmissionService>();

        // Konversi setara (M4.1, CLAUDE.md 7.5): angka faktor hanya dari tabel equivalences.
        services.AddSingleton<IEquivalenceRepository, EquivalenceRepository>();
        services.AddSingleton<IEquivalenceCalculator, EquivalenceCalculator>();
        services.AddSingleton<IEmissionRecordedHandler, EquivalenceRecordedHandler>();

        // Hutan virtual (M5.1, CLAUDE.md 7.7): handler hutan didaftarkan SETELAH handler konversi setara.
        services.AddSingleton<IForestLogRepository, ForestLogRepository>();
        services.AddSingleton<IForestService, ForestService>();
        services.AddSingleton<IEmissionRecordedHandler, ForestRecordedHandler>();

        // Laporan per periode (M6.1, CLAUDE.md 7.8): data selalu milik pengguna di sesi login.
        services.AddSingleton<IReportRepository, ReportRepository>();
        services.AddSingleton<IReportService, ReportService>();

        // Bagikan ke Instagram Story (M6.2, CLAUDE.md 7.9). Renderer PNG sebenarnya dari proyek WPF;
        // TryAdd agar renderer WPF tetap dipakai walau didaftarkan sebelum AddKeranjangHijauBackend.
        services.AddSingleton<ISharedReportRepository, SharedReportRepository>();
        services.TryAddSingleton<IShareCardRenderer, NotConfiguredShareCardRenderer>();
        services.AddSingleton<IShareService, ShareService>();

        // Layanan admin (M7.1, CLAUDE.md bagian 2): hanya baca, setiap method memeriksa sesi admin.
        services.AddSingleton<IAdminQueryRepository, AdminQueryRepository>();
        services.AddSingleton<IAdminService, AdminService>();

        // Milestone berikutnya menambahkan service di sini.

        return services;
    }

    private static void AddOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSectionOptions<DatabaseOptions>(configuration, DatabaseOptions.SectionName);

        services.AddSectionOptions<FeatureOptions>(configuration, FeatureOptions.SectionName);

        services.AddSectionOptions<EmissionOptions>(configuration, EmissionOptions.SectionName)
            .PostConfigure(o =>
            {
                if (o.ExpressKeywords.Count == 0)
                {
                    o.ExpressKeywords.AddRange(EmissionOptions.DefaultExpressKeywords);
                }
            })
            .Validate(o => o.RouteCorrectionFactor >= 1, "Emission:RouteCorrectionFactor minimal 1.")
            .Validate(o => o.MinimumDistanceKm > 0, "Emission:MinimumDistanceKm harus lebih dari 0.")
            .Validate(o => o.MaxWeightKg > 0, "Emission:MaxWeightKg harus lebih dari 0.")
            .Validate(o => o.DefaultWeightKg > 0 && o.DefaultWeightKg <= o.MaxWeightKg,
                "Emission:DefaultWeightKg harus lebih dari 0 dan tidak melebihi MaxWeightKg.")
            .Validate(o => o.ExpressKeywords.TrueForAll(k => !string.IsNullOrWhiteSpace(k)),
                "Emission:ExpressKeywords tidak boleh berisi kata kunci kosong.");

        services.AddSectionOptions<ForestOptions>(configuration, ForestOptions.SectionName)
            .Validate(o => o.PointsPer100GramCo2e >= 0, "Forest:PointsPer100GramCo2e tidak boleh negatif.")
            .Validate(o => o.RecoveryPointsPerDay >= 0, "Forest:RecoveryPointsPerDay tidak boleh negatif.")
            .Validate(o => o.InitialHealth is > 0 and <= 100, "Forest:InitialHealth harus di antara 0 dan 100.")
            .Validate(o => o.InitialTrees > 0, "Forest:InitialTrees harus lebih dari 0.");

        services.AddSectionOptions<CourierApiOptions>(configuration, CourierApiOptions.SectionName)
            .PostConfigure(o =>
            {
                if (o.SupportedCouriers.Count == 0)
                {
                    o.SupportedCouriers.AddRange(CourierApiOptions.DefaultSupportedCouriers);
                }
            })
            .Validate(o => o.TimeoutSeconds is >= 1 and <= 120, "CourierApi:TimeoutSeconds harus di antara 1 dan 120.")
            .Validate(
                o => string.Equals(o.Provider?.Trim(), CourierApiOptions.RajaOngkirProvider, StringComparison.OrdinalIgnoreCase)
                    || o.UsesGenericProvider,
                "CourierApi:Provider harus \"RajaOngkir\" atau \"Generic\".")
            .Validate(o => o.SupportedCouriers.TrueForAll(k => !string.IsNullOrWhiteSpace(k)),
                "CourierApi:SupportedCouriers tidak boleh berisi kode kurir kosong.");
    }

    private static OptionsBuilder<TOptions> AddSectionOptions<TOptions>(
        this IServiceCollection services, IConfiguration configuration, string sectionName)
        where TOptions : class =>
        services.AddOptions<TOptions>()
            .Configure(o => configuration.GetSection(sectionName).Bind(o));
}
