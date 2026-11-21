using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace KeranjangHijau.Tests.Integration;

/// <summary>Repository <c>shared_reports</c> (M6.2). Semua perubahan di-rollback setelah test.</summary>
public class SharedReportRepositoryTests : RepositoryTestBase
{
    private static readonly DateTimeOffset Mulai = new(2026, 10, 31, 17, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Selesai = new(2026, 11, 30, 17, 0, 0, TimeSpan.Zero);

    private ISharedReportRepository SharedReports => Provider.GetRequiredService<ISharedReportRepository>();

    private static SharedReport NewReport(int userId, DateTimeOffset createdAt, string? file = @"C:\Users\budi\Pictures\keranjanghijau-bulanan-20261120093015.png") => new()
    {
        UserId = userId,
        PeriodeMulai = Mulai,
        PeriodeSelesai = Selesai,
        JumlahPaket = 3,
        TotalCo2eKg = 1.2345m,
        FileGambar = file,
        CreatedAt = createdAt,
    };

    [DatabaseFact]
    public async Task Insert_ThenGetByUserId_RoundTrips()
    {
        var userId = await Users.InsertAsync(NewUser(), Transaction);
        var created = new DateTimeOffset(2026, 11, 20, 2, 30, 15, TimeSpan.Zero);

        var id = await SharedReports.InsertAsync(NewReport(userId, created), Transaction);

        var row = Assert.Single(await SharedReports.GetByUserIdAsync(userId, transaction: Transaction));
        Assert.True(id > 0);
        Assert.Equal(id, row.ShareId);
        Assert.Equal(userId, row.UserId);
        Assert.Equal(Mulai, row.PeriodeMulai);
        Assert.Equal(Selesai, row.PeriodeSelesai);
        Assert.Equal(3, row.JumlahPaket);
        Assert.Equal(1.2345m, row.TotalCo2eKg);
        Assert.Equal(@"C:\Users\budi\Pictures\keranjanghijau-bulanan-20261120093015.png", row.FileGambar);
        Assert.Equal(created, row.CreatedAt);
    }

    [DatabaseFact]
    public async Task GetByUserId_NewestFirst_OnlyOwnRows_RespectsLimit()
    {
        var userId = await Users.InsertAsync(NewUser(), Transaction);
        var otherId = await Users.InsertAsync(NewUser(), Transaction);
        var t = new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);

        var oldest = await SharedReports.InsertAsync(NewReport(userId, t), Transaction);
        var newest = await SharedReports.InsertAsync(NewReport(userId, t.AddHours(2)), Transaction);
        var middle = await SharedReports.InsertAsync(NewReport(userId, t.AddHours(1)), Transaction);
        await SharedReports.InsertAsync(NewReport(otherId, t.AddHours(3)), Transaction);

        var all = await SharedReports.GetByUserIdAsync(userId, transaction: Transaction);
        var top2 = await SharedReports.GetByUserIdAsync(userId, 2, Transaction);

        Assert.Equal([newest, middle, oldest], all.Select(r => r.ShareId));
        Assert.Equal([newest, middle], top2.Select(r => r.ShareId));
        Assert.Empty(await SharedReports.GetByUserIdAsync(-1, transaction: Transaction));
    }

    [DatabaseFact]
    public async Task Insert_EmptyFileAndDefaultTime_StoresNullAndNow()
    {
        var userId = await Users.InsertAsync(NewUser(), Transaction);

        await SharedReports.InsertAsync(NewReport(userId, default, "   "), Transaction);

        var row = Assert.Single(await SharedReports.GetByUserIdAsync(userId, transaction: Transaction));
        Assert.Null(row.FileGambar);
        Assert.True(row.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-5));
    }

    [DatabaseFact]
    public async Task Insert_UnknownUser_ThrowsNotFound()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => SharedReports.InsertAsync(NewReport(-1, DateTimeOffset.UtcNow), Transaction));

        Assert.Equal("Pengguna tidak ditemukan.", ex.Message);
    }

    [DatabaseFact]
    public async Task Insert_InvalidValues_ThrowBeforeTouchingDb()
    {
        var userId = await Users.InsertAsync(NewUser(), Transaction);
        var now = DateTimeOffset.UtcNow;

        var negative = NewReport(userId, now);
        negative.JumlahPaket = -1;
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => SharedReports.InsertAsync(negative, Transaction));

        var reversed = NewReport(userId, now);
        reversed.PeriodeSelesai = Mulai.AddDays(-1);
        await Assert.ThrowsAsync<ArgumentException>(() => SharedReports.InsertAsync(reversed, Transaction));

        await Assert.ThrowsAsync<ArgumentException>(
            () => SharedReports.InsertAsync(NewReport(userId, now, new string('x', 261)), Transaction));

        // 260 karakter masih diterima.
        await SharedReports.InsertAsync(NewReport(userId, now, new string('x', 260)), Transaction);
        Assert.Single(await SharedReports.GetByUserIdAsync(userId, transaction: Transaction));
    }

    [DatabaseFact]
    public async Task GetByUserId_LimitBelowOne_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => SharedReports.GetByUserIdAsync(1, 0, Transaction));
    }
}
