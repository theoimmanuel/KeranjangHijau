using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Core.Rules;
using KeranjangHijau.Core.Services;
using KeranjangHijau.Infrastructure.Tracking;
using KeranjangHijau.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Tests.Services;

public class EmissionServiceTests
{
    private static readonly UserDto Pengguna = new(10, "Budi", "budi@contoh.id", UserRole.Pengguna, DateTimeOffset.UnixEpoch);
    private static readonly UserDto Admin = new(1, "Admin", "admin@keranjanghijau.local", UserRole.Admin, DateTimeOffset.UnixEpoch);

    private readonly InMemoryCurrentUserSession _session = new();
    private readonly InMemoryCourierRepository _couriers = new();
    private readonly InMemoryShipmentRepository _shipments = new();
    private readonly InMemoryEmissionRecordRepository _records = new();
    private readonly InMemoryCityRepository _cities = new();
    private readonly InMemoryEmissionFactorRepository _factors = new();
    private readonly FakeCourierTracker _fakeTracker = new();
    private readonly FakeTransactionRunner _transactions = new();
    private readonly FakeClock _clock = new();
    private readonly FeatureOptions _features = new() { AllowDuplicateResi = true };
    private readonly EmissionOptions _emissionOptions = new();
    private readonly List<IEmissionRecordedHandler> _handlers = [];
    private readonly InMemoryEquivalenceRepository _equivalences = new();

    public EmissionServiceTests()
    {
        _emissionOptions.ExpressKeywords.AddRange(EmissionOptions.DefaultExpressKeywords);
        _session.SignIn(Pengguna);
    }

    private HaversineDistanceCalculator Distance => new(Options.Create(_emissionOptions));

    private EmissionService CreateService(ICourierTracker? tracker = null) => new(
        _session,
        new ResiValidator(_couriers, _shipments, Options.Create(_features)),
        tracker ?? _fakeTracker,
        _cities,
        new WeightResolver(Options.Create(_emissionOptions)),
        Distance,
        new TransportModeResolver(Options.Create(_emissionOptions)),
        _factors,
        new LocalFormulaCalculator(),
        _shipments,
        _records,
        _transactions,
        _clock,
        _handlers);

    private void AddTracking(
        string resi = "JNE0000000001",
        string kotaAsal = "Jakarta",
        string kotaTujuan = "Bandung",
        string? layanan = "REG",
        decimal? beratKg = 2m,
        TrackingSource sumber = TrackingSource.Mock,
        string kodeKurir = "jne") =>
        _fakeTracker.Add(new TrackingResult(resi, kodeKurir, kotaAsal, kotaTujuan, layanan, beratKg, sumber));

    private static decimal ExpectedCo2e(decimal beratKg, decimal jarakKm, decimal factor) =>
        Math.Round(beratKg / 1000m * jarakKm * factor, 4, MidpointRounding.AwayFromZero);

    // ---------------------------------------------------------------- Alur sukses

    [Fact]
    public async Task Success_FromRealMockTracker_JakartaMedanYes()
    {
        // mock-resi.json: jne JNE1001000003, Jakarta → Medan, YES, 0,8 kg.
        var result = await CreateService(new MockCourierTracker())
            .CalculateFromResiAsync(new EmissionRequest("JNE", " jne 1001 000003 "));

        var expectedJarak = Distance.CalculateDistanceKm(InMemoryCityRepository.Jakarta, InMemoryCityRepository.Medan);

        Assert.Equal("JNE1001000003", result.NomorResi);
        Assert.Equal("jne", result.KodeKurir);
        Assert.Equal("JNE", result.NamaKurir);
        Assert.Equal("Jakarta", result.KotaAsal);
        Assert.Equal("Medan", result.KotaTujuan);
        Assert.Equal("YES", result.Layanan);
        Assert.Equal(0.8m, result.BeratKg);
        Assert.Equal(WeightSource.Api, result.SumberBerat);
        Assert.Equal(ShipmentDataSource.Mock, result.SumberData);
        Assert.Equal(expectedJarak, result.JarakKm);
        Assert.Equal(TransportMode.Pesawat, result.ModaTransport);
        Assert.Equal(ExpectedCo2e(0.8m, expectedJarak, 0.8m), result.TotalCo2eKg);
        Assert.True(result.TotalCo2eKg > 0);
        Assert.Equal(EmissionMethod.RumusLokal, result.Metode);
        Assert.Equal(_clock.UtcNow, result.CalculatedAt);
    }

    [Fact]
    public async Task Success_SavesShipmentAndRecord()
    {
        AddTracking(beratKg: 2m);

        var result = await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        var shipment = Assert.Single(_shipments.Shipments);
        Assert.Equal(result.ShipmentId, shipment.ShipmentId);
        Assert.Equal(Pengguna.UserId, shipment.UserId);
        Assert.Equal(1, shipment.CourierId);
        Assert.Equal("JNE0000000001", shipment.NomorResi);
        Assert.Equal(InMemoryCityRepository.Jakarta.CityId, shipment.OriginCityId);
        Assert.Equal(InMemoryCityRepository.Bandung.CityId, shipment.DestCityId);
        Assert.Equal("REG", shipment.Layanan);
        Assert.Equal(2m, shipment.BeratKg);
        Assert.Equal(ShipmentDataSource.Mock, shipment.SumberData);
        Assert.Equal(_clock.UtcNow, shipment.CreatedAt);

        var record = Assert.Single(_records.Records);
        Assert.Equal(result.RecordId, record.RecordId);
        Assert.Equal(shipment.ShipmentId, record.ShipmentId);
        Assert.Equal(_factors.Factors[TransportMode.Truk].FactorId, record.FactorId);
        Assert.Equal(result.JarakKm, record.JarakKm);
        Assert.Equal(result.TotalCo2eKg, record.TotalCo2eKg);
        Assert.Equal(EmissionMethod.RumusLokal, record.Metode);
        Assert.Equal(_clock.UtcNow, record.CalculatedAt);
    }

    [Fact]
    public async Task Success_TrukWithinJava_UsesFormula()
    {
        AddTracking(kotaAsal: "Jakarta", kotaTujuan: "Surabaya", layanan: "REG", beratKg: 2m);

        var result = await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        var jarak = Distance.CalculateDistanceKm(InMemoryCityRepository.Jakarta, InMemoryCityRepository.Surabaya);
        Assert.Equal(TransportMode.Truk, result.ModaTransport);
        Assert.Equal(ExpectedCo2e(2m, jarak, 0.1m), result.TotalCo2eKg);
    }

    [Fact]
    public async Task ShipmentAndRecord_AreWrittenInOneCommittedTransaction()
    {
        AddTracking();

        await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        Assert.Equal(1, _transactions.Commits);
        Assert.Equal(0, _transactions.Rollbacks);
        Assert.NotNull(_transactions.LastTransaction);
        Assert.Same(_transactions.LastTransaction, Assert.Single(_shipments.InsertTransactions));
        Assert.Same(_transactions.LastTransaction, Assert.Single(_records.InsertTransactions));
    }

    [Fact]
    public async Task SameCity_UsesMinimumDistance()
    {
        AddTracking(kotaAsal: "Bandung", kotaTujuan: "bandung", beratKg: 1m);

        var result = await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        Assert.Equal(10m, result.JarakKm);
        Assert.Equal(TransportMode.Truk, result.ModaTransport);
        Assert.Equal(0.001m, result.TotalCo2eKg); // (1 / 1000) × 10 × 0,1
    }

    [Fact]
    public async Task DifferentIsland_NoService_IsKapal_AndStoredAsNull()
    {
        AddTracking(kotaAsal: "Surabaya", kotaTujuan: "Makassar", layanan: "  ", beratKg: 1m);

        var result = await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        Assert.Equal(TransportMode.Kapal, result.ModaTransport);
        Assert.Null(result.Layanan);
        Assert.Null(Assert.Single(_shipments.Shipments).Layanan);
    }

    [Fact]
    public async Task LongServiceName_IsTruncatedToColumnLength()
    {
        AddTracking(layanan: new string('R', 80));

        var result = await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        Assert.Equal(50, result.Layanan!.Length);
    }

    // ---------------------------------------------------------------- Berat dan sumber_data

    [Fact]
    public async Task NoTrackerWeight_ManualWeight_IsUsed_AndSourceIsManual()
    {
        AddTracking(beratKg: null);

        var result = await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001", 3.5m));

        Assert.Equal(3.5m, result.BeratKg);
        Assert.Equal(WeightSource.Manual, result.SumberBerat);
        Assert.Equal(ShipmentDataSource.Manual, result.SumberData);
        Assert.Equal(ShipmentDataSource.Manual, Assert.Single(_shipments.Shipments).SumberData);
    }

    [Fact]
    public async Task TrackerWeight_WinsOverManual_AndSourceFollowsTracker()
    {
        AddTracking(beratKg: 2m, sumber: TrackingSource.Api);

        var result = await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001", 9m));

        Assert.Equal(2m, result.BeratKg);
        Assert.Equal(WeightSource.Api, result.SumberBerat);
        Assert.Equal(ShipmentDataSource.Api, result.SumberData);
    }

    [Fact]
    public async Task NoWeightAtAll_UsesDefault_AndSourceFollowsTracker()
    {
        AddTracking(beratKg: null, sumber: TrackingSource.Mock);

        var result = await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        Assert.Equal(1m, result.BeratKg);
        Assert.Equal(WeightSource.Default, result.SumberBerat);
        Assert.Equal(ShipmentDataSource.Mock, result.SumberData);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task InvalidManualWeight_Throws_AndNothingIsSaved(double berat)
    {
        AddTracking(beratKg: null);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001", (decimal)berat)));

        Assert.Equal("BeratKg", ex.Field);
        Assert.Empty(_shipments.Shipments);
        Assert.Null(_transactions.LastTransaction);
    }

    // ---------------------------------------------------------------- Resi tidak ditemukan / tidak valid

    [Fact]
    public async Task ResiNotFound_ThrowsNotFound_AndNothingIsSaved()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE9999999999")));

        Assert.Equal("Data resi tidak ditemukan.", ex.Message);
        Assert.Empty(_shipments.Shipments);
        Assert.Empty(_records.Records);
        Assert.Null(_transactions.LastTransaction);
    }

    [Fact]
    public async Task InvalidResiFormat_Throws_BeforeTracker()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "ABC")));

        Assert.Equal("NomorResi", ex.Field);
        Assert.Equal(0, _fakeTracker.CallCount);
    }

    [Fact]
    public async Task UnknownCourier_Throws_BeforeTracker()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().CalculateFromResiAsync(new EmissionRequest("kurirfiktif", "JNE0000000001")));

        Assert.Equal("KodeKurir", ex.Field);
        Assert.Equal(0, _fakeTracker.CallCount);
    }

    // ---------------------------------------------------------------- Kota tidak dikenal

    [Fact]
    public async Task UnknownOriginCity_ThrowsValidation_NamingTheCity()
    {
        AddTracking(kotaAsal: "Kota Fiksi", kotaTujuan: "Bandung");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001")));

        Assert.Contains("Kota Fiksi", ex.Message);
        Assert.Equal("Kota asal \"Kota Fiksi\" belum terdaftar di sistem.", ex.Message);
        Assert.Equal(EmissionService.KotaAsalField, ex.Field);
        Assert.Empty(_shipments.Shipments);
        Assert.Null(_transactions.LastTransaction);
    }

    [Fact]
    public async Task UnknownDestinationCity_ThrowsValidation_NamingTheCity()
    {
        AddTracking(kotaAsal: "Jakarta", kotaTujuan: "Atlantis");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001")));

        Assert.Equal("Kota tujuan \"Atlantis\" belum terdaftar di sistem.", ex.Message);
        Assert.Equal(EmissionService.KotaTujuanField, ex.Field);
    }

    // ---------------------------------------------------------------- Sesi dan peran

    [Fact]
    public async Task NotLoggedIn_ThrowsUnauthorized_BeforeAnything()
    {
        _session.SignOut();
        AddTracking();

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001")));

        Assert.Equal(EmissionService.NotLoggedInMessage, ex.Message);
        Assert.Equal(0, _fakeTracker.CallCount);
        Assert.Empty(_shipments.Shipments);
    }

    [Fact]
    public async Task Admin_IsRejected()
    {
        _session.SignIn(Admin);
        AddTracking();

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001")));

        Assert.Equal(EmissionService.NotPenggunaMessage, ex.Message);
        Assert.Equal(0, _fakeTracker.CallCount);
        Assert.Empty(_shipments.Shipments);
    }

    // ---------------------------------------------------------------- Duplikat resi

    [Fact]
    public async Task DuplicateResi_IsAllowed_WhenAllowDuplicateResiTrue()
    {
        _features.AllowDuplicateResi = true;
        AddTracking();
        var service = CreateService();

        var first = await service.CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));
        var second = await service.CalculateFromResiAsync(new EmissionRequest("jne", "jne 0000000001"));

        Assert.NotEqual(first.ShipmentId, second.ShipmentId);
        Assert.NotEqual(first.RecordId, second.RecordId);
        Assert.Equal(2, _shipments.Shipments.Count);
        Assert.Equal(2, _records.Records.Count);
        Assert.All(_shipments.Shipments, s => Assert.Equal("JNE0000000001", s.NomorResi));
    }

    [Fact]
    public async Task DuplicateResi_IsRejected_WhenAllowDuplicateResiFalse()
    {
        _features.AllowDuplicateResi = false;
        AddTracking();
        var service = CreateService();
        await service.CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        await Assert.ThrowsAsync<DuplicateResiException>(
            () => service.CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001")));

        Assert.Single(_shipments.Shipments);
        Assert.Equal(1, _fakeTracker.CallCount);
    }

    // ---------------------------------------------------------------- Faktor emisi hilang

    [Fact]
    public async Task MissingEmissionFactor_ThrowsNotFound()
    {
        _factors.Factors.Remove(TransportMode.Truk);
        AddTracking();

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001")));

        Assert.Contains("truk", ex.Message);
        Assert.Empty(_shipments.Shipments);
    }

    // ---------------------------------------------------------------- Titik perluasan

    [Fact]
    public async Task Handler_IsCalledOnce_InSameTransaction_WithSavedData()
    {
        var handler = new RecordingEmissionHandler();
        _handlers.Add(handler);
        AddTracking();

        var result = await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        var (context, tx) = Assert.Single(handler.Calls);
        Assert.Same(_transactions.LastTransaction, tx);
        Assert.Equal(Pengguna, context.User);
        Assert.Equal(result, context.Result);
        Assert.Equal(result.ShipmentId, context.Shipment.ShipmentId);
        Assert.Equal(result.RecordId, context.Record.RecordId);
        Assert.Equal(context.Shipment.ShipmentId, context.Record.ShipmentId);
        Assert.Equal(result.TotalCo2eKg, context.Record.TotalCo2eKg);
    }

    [Fact]
    public async Task Handlers_AreCalledInRegistrationOrder()
    {
        var order = new List<string>();
        _handlers.Add(new RecordingEmissionHandler("setara", order));
        _handlers.Add(new RecordingEmissionHandler("hutan", order));
        AddTracking();

        await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        Assert.Equal(["setara", "hutan"], order);
    }

    [Fact]
    public async Task HandlerFailure_RollsBackTransaction_AndPropagates()
    {
        _handlers.Add(new RecordingEmissionHandler { FailWith = new InvalidOperationException("gagal") });
        AddTracking();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001")));

        Assert.Equal(0, _transactions.Commits);
        Assert.Equal(1, _transactions.Rollbacks);
    }

    // ---------------------------------------------------------------- Konversi setara (M4.1)

    private void AddEquivalenceHandler()
    {
        // Angka test (bukan angka seed).
        _equivalences.Equivalences.Add(new KeranjangHijau.Core.Models.Equivalence
        {
            EquivalenceId = 1, Aktivitas = "Aktivitas A", Satuan = "km", KgCo2ePerSatuan = 0.05m, Ikon = "a",
            Sumber = "PLACEHOLDER - ganti dari jurnal",
        });
        _equivalences.Equivalences.Add(new KeranjangHijau.Core.Models.Equivalence
        {
            EquivalenceId = 2, Aktivitas = "Aktivitas B", Satuan = "km", KgCo2ePerSatuan = 0.2m, Ikon = "b",
            Sumber = "Jurnal X (2025)",
        });
        _handlers.Add(new EquivalenceRecordedHandler(_equivalences, new EquivalenceCalculator()));
    }

    [Fact]
    public async Task EquivalenceHandler_ResultContainsEquivalences_AndRowsAreSavedInSameTransaction()
    {
        AddEquivalenceHandler();
        AddTracking(beratKg: 2m);

        var result = await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        var expectedA = Math.Round(result.TotalCo2eKg / 0.05m, 2, MidpointRounding.AwayFromZero);
        var expectedB = Math.Round(result.TotalCo2eKg / 0.2m, 2, MidpointRounding.AwayFromZero);
        Assert.True(result.TotalCo2eKg > 0);
        Assert.Equal(
            [
                new EquivalenceDto("Aktivitas A", "km", expectedA, "a", true),
                new EquivalenceDto("Aktivitas B", "km", expectedB, "b", false),
            ],
            result.KonversiSetara);

        Assert.Equal(
            [(result.RecordId, 1, expectedA), (result.RecordId, 2, expectedB)],
            _equivalences.Rows.Select(r => (r.RecordId, r.EquivalenceId, r.NilaiSetara)));
        Assert.Same(_transactions.LastTransaction, Assert.Single(_equivalences.InsertTransactions));
        Assert.Equal(1, _transactions.Commits);
    }

    [Fact]
    public async Task EquivalenceHandler_LaterHandlerSeesEnrichedResult()
    {
        AddEquivalenceHandler();
        var later = new RecordingEmissionHandler();
        _handlers.Add(later);
        AddTracking();

        var result = await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        Assert.Equal(2, Assert.Single(later.Calls).Context.Result.KonversiSetara.Count);
        Assert.Same(later.Calls[0].Context.Result, result);
    }

    [Fact]
    public async Task EquivalenceHandler_Failure_RollsBackEmission()
    {
        AddEquivalenceHandler();
        _equivalences.FailOnInsert = new InvalidOperationException("gagal simpan");
        AddTracking();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001")));

        Assert.Equal(0, _transactions.Commits);
        Assert.Equal(1, _transactions.Rollbacks);
    }

    [Fact]
    public async Task WithoutEquivalenceHandler_ResultHasEmptyList()
    {
        AddTracking();

        var result = await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        Assert.Empty(result.KonversiSetara);
    }

    [Fact]
    public async Task NoHandlers_StillWorks()
    {
        AddTracking();

        var result = await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        Assert.True(result.RecordId > 0);
    }

    [Fact]
    public async Task NullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateService().CalculateFromResiAsync(null!));
    }
    // ---------------------------------------------------------------- Nama kota format API (M2.4)

    [Theory]
    [InlineData("KOTA ADM. JAKARTA BARAT", "KOTA BANDUNG")]
    [InlineData("JAKARTA BARAT", "BANDUNG, JAWA BARAT")]
    [InlineData("jakarta", "Kab. Bandung")]
    public async Task ApiStyleCityNames_AreMatchedToSeedCities(string kotaAsal, string kotaTujuan)
    {
        AddTracking(kotaAsal: kotaAsal, kotaTujuan: kotaTujuan, sumber: TrackingSource.Api);

        var result = await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001"));

        Assert.Equal("Jakarta", result.KotaAsal);
        Assert.Equal("Bandung", result.KotaTujuan);
        var shipment = Assert.Single(_shipments.Shipments);
        Assert.Equal(InMemoryCityRepository.Jakarta.CityId, shipment.OriginCityId);
        Assert.Equal(InMemoryCityRepository.Bandung.CityId, shipment.DestCityId);
        Assert.Equal(1, _cities.GetAllCallCount); // daftar kota dibaca sekali untuk asal dan tujuan
    }

    [Fact]
    public async Task UnknownApiStyleCity_StillNamesTheCityInMessage()
    {
        AddTracking(kotaAsal: "KAB. SLEMAN", kotaTujuan: "Bandung", sumber: TrackingSource.Api);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001")));

        Assert.Equal("Kota asal \"KAB. SLEMAN\" belum terdaftar di sistem.", ex.Message);
        Assert.Equal(EmissionService.KotaAsalField, ex.Field);
    }

    // ---------------------------------------------------------------- Nomor HP opsional (M2.4)

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyPhoneSuffix_IsNotSentToTracker(string? hp)
    {
        AddTracking();

        await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001") { NomorHpAkhir = hp });

        Assert.NotNull(_fakeTracker.LastRequest);
        Assert.Null(_fakeTracker.LastRequest.NomorHpAkhir);
    }

    [Fact]
    public async Task PhoneSuffix_IsTrimmedAndPassedToTracker()
    {
        AddTracking();

        await CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "jne 0000000001") { NomorHpAkhir = " 12345 " });

        Assert.Equal(new TrackingRequest("jne", "JNE0000000001", "12345"), _fakeTracker.LastRequest);
        Assert.Single(_shipments.Shipments);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("12a45")]
    [InlineData("0812-3456-7890")]
    public async Task InvalidPhoneSuffix_ThrowsValidation_WithoutTheNumber(string hp)
    {
        AddTracking();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().CalculateFromResiAsync(new EmissionRequest("jne", "JNE0000000001") { NomorHpAkhir = hp }));

        Assert.Equal("Isi 5 digit terakhir nomor HP penerima.", ex.Message);
        Assert.Equal("NomorHpAkhir", ex.Field);
        Assert.DoesNotContain(hp, ex.Message);
        Assert.DoesNotContain(hp, ex.ToString());
        Assert.Equal(0, _fakeTracker.CallCount);
        Assert.Empty(_shipments.Shipments);
    }
}
