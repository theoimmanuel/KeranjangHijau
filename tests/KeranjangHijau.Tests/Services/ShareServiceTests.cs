using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;
using KeranjangHijau.Core.Services;
using KeranjangHijau.Tests.Fakes;

namespace KeranjangHijau.Tests.Services;

/// <summary><see cref="ShareService"/> dengan <see cref="ReportService"/> asli di atas repository in-memory.</summary>
public sealed class ShareServiceTests : IDisposable
{
    private const int UserId = 10;
    private const int MotorId = 1;
    private const int MobilId = 2;

    private readonly InMemoryCurrentUserSession _session = new();
    private readonly InMemoryReportRepository _reportData = new();
    private readonly FakeForestService _forests = new();
    private readonly FakeShareCardRenderer _renderer = new();
    private readonly InMemorySharedReportRepository _shared = new();
    private readonly TempFolder _folder = new();

    // 20 November 2026 09.30.15 WIB.
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 20, 2, 30, 15, TimeSpan.Zero));

    public ShareServiceTests()
    {
        _reportData.Activities.Add(new Equivalence { EquivalenceId = MotorId, Aktivitas = "Naik motor", Satuan = "km", KgCo2ePerSatuan = 0.07m, Ikon = "motor", Sumber = "PLACEHOLDER - ganti dari jurnal" });
        _reportData.Activities.Add(new Equivalence { EquivalenceId = MobilId, Aktivitas = "Naik mobil", Satuan = "km", KgCo2ePerSatuan = 0.17m, Ikon = "mobil", Sumber = "PLACEHOLDER - ganti dari jurnal" });
        _forests.Forests[UserId] = new ForestDto("Hutan Budi", 72.5m, ForestStatus.Sehat, 100, 73);
    }

    public void Dispose() => _folder.Dispose();

    private ShareService CreateService() => new(
        _session,
        new ReportService(_session, _reportData, _forests, _clock),
        _renderer,
        _shared,
        _clock);

    private void LoginAs(UserRole role = UserRole.Pengguna) =>
        _session.SignIn(new UserDto(UserId, "Budi", "budi@test.id", role, _clock.UtcNow));

    private static DateTimeOffset Utc(int y, int mo, int d, int h = 0) => new(y, mo, d, h, 0, 0, TimeSpan.Zero);

    /// <summary>Dua paket November 2026 (minggu 16–22 Nov) + satu paket Oktober.</summary>
    private void AddPackages()
    {
        _reportData.Add(UserId, Utc(2026, 11, 17, 3), 1.25m, new Dictionary<int, decimal> { [MotorId] = 17.86m, [MobilId] = 7.35m });
        _reportData.Add(UserId, Utc(2026, 11, 19, 3), 0.5m, new Dictionary<int, decimal> { [MotorId] = 7.14m, [MobilId] = 2.94m });
        _reportData.Add(UserId, Utc(2026, 10, 5, 3), 2m, new Dictionary<int, decimal> { [MotorId] = 28.57m, [MobilId] = 11.76m });
    }

    // =================================================================== Isi kartu

    [Fact]
    public async Task BuildCardData_Monthly_HasCorrectContent()
    {
        LoginAs();
        AddPackages();

        var card = await CreateService().BuildCardDataAsync(ReportPeriod.Bulanan);

        Assert.Equal("Budi", card.NamaPengguna);
        Assert.Equal(ReportPeriod.Bulanan, card.Periode);
        Assert.Equal("November 2026", card.LabelPeriode);
        Assert.Equal(2, card.JumlahPaket);
        Assert.Equal(1.75m, card.TotalCo2eKg);
        Assert.Equal(new EquivalenceDto("Naik motor", "km", 25m, "motor", true), card.KonversiUtama);
        Assert.Equal("Hutan Budi", card.NamaHutan);
        Assert.Equal(ForestStatus.Sehat, card.StatusHutan);
        Assert.Equal(72.5m, card.KesehatanHutan);
        Assert.Equal("Yuk cek jejak karbon paketmu di KeranjangHijau!", card.KalimatAjakan);
        Assert.Equal(1080, card.LebarPx);
        Assert.Equal(1920, card.TinggiPx);
    }

    [Fact]
    public async Task BuildCardData_WeeklyAndAll_UseMatchingLabels()
    {
        LoginAs();
        AddPackages();
        var service = CreateService();

        var weekly = await service.BuildCardDataAsync(ReportPeriod.Mingguan);
        var all = await service.BuildCardDataAsync(ReportPeriod.Semua);
        var october = await service.BuildCardDataAsync(ReportPeriod.Bulanan, new DateOnly(2026, 10, 1));

        Assert.Equal("16–22 November 2026", weekly.LabelPeriode);
        Assert.Equal(2, weekly.JumlahPaket);
        Assert.Equal("Sejak 5 Oktober 2026", all.LabelPeriode);
        Assert.Equal(3, all.JumlahPaket);
        Assert.Equal(3.75m, all.TotalCo2eKg);
        Assert.Equal("Oktober 2026", october.LabelPeriode);
        Assert.Equal(28.57m, october.KonversiUtama!.NilaiSetara);
    }

    [Fact]
    public async Task BuildCardData_NoEquivalenceActivities_KonversiUtamaIsNull()
    {
        _reportData.Activities.Clear();
        LoginAs();
        AddPackages();

        var card = await CreateService().BuildCardDataAsync(ReportPeriod.Bulanan);

        Assert.Null(card.KonversiUtama);
    }

    // =================================================================== Laporan kosong dan sesi

    [Fact]
    public async Task BuildCardData_EmptyReport_IsRejected()
    {
        LoginAs();
        AddPackages();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().BuildCardDataAsync(ReportPeriod.Bulanan, new DateOnly(2026, 9, 1)));

        Assert.Equal("Belum ada paket di periode ini untuk dibagikan.", ex.Message);
    }

    [Fact]
    public async Task Share_EmptyReport_IsRejected_WithoutRenderingOrSaving()
    {
        LoginAs();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateService().ShareAsync(ReportPeriod.Mingguan, _folder.Path));

        Assert.Equal(ShareService.EmptyReportMessage, ex.Message);
        Assert.Empty(_renderer.Calls);
        Assert.Empty(_shared.Reports);
        Assert.False(Directory.Exists(_folder.Path));
    }

    [Fact]
    public async Task NotLoggedIn_OrAdmin_IsUnauthorized()
    {
        AddPackages();
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.BuildCardDataAsync(ReportPeriod.Semua));
        await Assert.ThrowsAsync<UnauthorizedException>(() => service.ShareAsync(ReportPeriod.Semua, _folder.Path));

        LoginAs(UserRole.Admin);
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => service.ShareAsync(ReportPeriod.Semua, _folder.Path));
        Assert.Equal(ReportService.NotPenggunaMessage, ex.Message);
        Assert.Empty(_renderer.Calls);
        Assert.Empty(_shared.Reports);
    }

    // =================================================================== Bagikan

    [Fact]
    public async Task Share_RendersCard_SavesHistory_ReturnsPath()
    {
        LoginAs();
        AddPackages();

        var path = await CreateService().ShareAsync(ReportPeriod.Bulanan, _folder.Path);

        var expectedPath = Path.Combine(Path.GetFullPath(_folder.Path), "keranjanghijau-bulanan-20261120093015.png");
        Assert.Equal(expectedPath, path);
        Assert.True(File.Exists(path));
        Assert.True(ShareFileNameRule.IsValid(Path.GetFileName(path)));

        var (data, outputPath) = Assert.Single(_renderer.Calls);
        Assert.Equal(expectedPath, outputPath);
        Assert.Equal("November 2026", data.LabelPeriode);
        Assert.Equal(1.75m, data.TotalCo2eKg);

        var row = Assert.Single(_shared.Reports);
        Assert.Equal(UserId, row.UserId);
        Assert.Equal(PeriodRangeRule.StartOfJakartaDayUtc(new DateOnly(2026, 11, 1)), row.PeriodeMulai);
        Assert.Equal(PeriodRangeRule.StartOfJakartaDayUtc(new DateOnly(2026, 12, 1)), row.PeriodeSelesai);
        Assert.Equal(2, row.JumlahPaket);
        Assert.Equal(1.75m, row.TotalCo2eKg);
        Assert.Equal(path, row.FileGambar);
        Assert.Equal(_clock.UtcNow, row.CreatedAt);
    }

    [Fact]
    public async Task Share_All_StoresPeriodFromOldestPackage()
    {
        LoginAs();
        AddPackages();

        await CreateService().ShareAsync(ReportPeriod.Semua, _folder.Path);

        var row = Assert.Single(_shared.Reports);
        Assert.Equal(PeriodRangeRule.StartOfJakartaDayUtc(new DateOnly(2026, 10, 5)), row.PeriodeMulai);
        Assert.Equal(PeriodRangeRule.StartOfJakartaDayUtc(new DateOnly(2026, 11, 21)), row.PeriodeSelesai);
        Assert.Equal(3, row.JumlahPaket);
    }

    [Fact]
    public async Task Share_TwiceInSameSecond_UsesUniqueFileNames_AndKeepsBothRows()
    {
        LoginAs();
        AddPackages();
        var service = CreateService();

        var first = await service.ShareAsync(ReportPeriod.Mingguan, _folder.Path);
        var second = await service.ShareAsync(ReportPeriod.Mingguan, _folder.Path);
        _clock.Advance(TimeSpan.FromSeconds(1));
        var third = await service.ShareAsync(ReportPeriod.Mingguan, _folder.Path);

        Assert.Equal("keranjanghijau-mingguan-20261120093015.png", Path.GetFileName(first));
        Assert.Equal("keranjanghijau-mingguan-20261120093015-1.png", Path.GetFileName(second));
        Assert.Equal("keranjanghijau-mingguan-20261120093016.png", Path.GetFileName(third));
        Assert.Equal(3, _shared.Reports.Count);
        Assert.Equal(3, Directory.GetFiles(_folder.Path).Length);

        var history = await _shared.GetByUserIdAsync(UserId);
        Assert.Equal([third, second, first], history.Select(r => r.FileGambar));
    }

    [Fact]
    public async Task Share_CreatesMissingNestedFolder()
    {
        LoginAs();
        AddPackages();
        var nested = Path.Combine(_folder.Path, "kartu", "story");

        var path = await CreateService().ShareAsync(ReportPeriod.Bulanan, nested);

        Assert.Equal(Path.GetFullPath(nested), Path.GetDirectoryName(path));
        Assert.True(File.Exists(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Share_EmptyFolder_IsRejected(string? folder)
    {
        LoginAs();
        AddPackages();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => CreateService().ShareAsync(ReportPeriod.Bulanan, folder!));

        Assert.Equal(ShareService.OutputFolderRequiredMessage, ex.Message);
        Assert.Equal(ShareService.OutputFolderField, ex.Field);
        Assert.Empty(_renderer.Calls);
    }

    [Fact]
    public async Task Share_FolderPathTooLong_IsRejected()
    {
        LoginAs();
        AddPackages();
        var longFolder = Path.Combine(_folder.Path, new string('a', 120), new string('b', 120));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => CreateService().ShareAsync(ReportPeriod.Bulanan, longFolder));

        Assert.Equal(ShareService.OutputPathTooLongMessage, ex.Message);
        Assert.Empty(_renderer.Calls);
        Assert.Empty(_shared.Reports);
    }

    [Fact]
    public async Task Share_FolderIsAFile_IsRejected()
    {
        LoginAs();
        AddPackages();
        Directory.CreateDirectory(_folder.Path);
        var file = Path.Combine(_folder.Path, "bukan-folder.txt");
        await File.WriteAllTextAsync(file, "x");

        var ex = await Assert.ThrowsAsync<ValidationException>(() => CreateService().ShareAsync(ReportPeriod.Bulanan, file));

        Assert.Equal(ShareService.OutputFolderUnavailableMessage, ex.Message);
    }

    [Fact]
    public async Task Share_RendererFails_NothingSaved()
    {
        LoginAs();
        AddPackages();
        _renderer.FailWith = new IOException("Disk penuh");

        await Assert.ThrowsAsync<IOException>(() => CreateService().ShareAsync(ReportPeriod.Bulanan, _folder.Path));

        Assert.Empty(_shared.Reports);
    }

    [Fact]
    public async Task Share_SaveFails_DeletesRenderedFile()
    {
        LoginAs();
        AddPackages();
        _shared.FailOnInsert = new NotFoundException("Pengguna tidak ditemukan.");

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().ShareAsync(ReportPeriod.Bulanan, _folder.Path));

        var (_, outputPath) = Assert.Single(_renderer.Calls);
        Assert.False(File.Exists(outputPath));
    }
}
