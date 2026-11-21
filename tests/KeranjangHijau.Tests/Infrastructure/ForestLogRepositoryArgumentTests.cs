using KeranjangHijau.Core.Models;
using KeranjangHijau.Infrastructure.Data;
using KeranjangHijau.Tests.Fakes;

namespace KeranjangHijau.Tests.Infrastructure;

/// <summary>Validasi argumen <see cref="ForestLogRepository"/> yang terjadi sebelum database disentuh.</summary>
public class ForestLogRepositoryArgumentTests
{
    private static readonly ForestLogRepository Repository = new(new ThrowingConnectionFactory(), new FakeClock());

    private static ForestLog Valid() => new() { ForestId = 1, RecordId = 1, Perubahan = -1m, KesehatanSetelah = 99m, Keterangan = "Emisi paket X" };

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Insert_EmptyKeterangan_Throws(string keterangan)
    {
        var log = Valid();
        log.Keterangan = keterangan;

        await Assert.ThrowsAsync<ArgumentException>(() => Repository.InsertAsync(log));
    }

    [Fact]
    public async Task Insert_KeteranganTooLong_Throws()
    {
        var log = Valid();
        log.Keterangan = new string('a', ForestLogRepository.MaxKeteranganLength + 1);

        await Assert.ThrowsAsync<ArgumentException>(() => Repository.InsertAsync(log));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public async Task Insert_HealthOutOfRange_Throws(double kesehatan)
    {
        var log = Valid();
        log.KesehatanSetelah = (decimal)kesehatan;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Repository.InsertAsync(log));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task GetByForestId_LimitBelowOne_Throws(int limit)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Repository.GetByForestIdAsync(1, limit));
    }

    private sealed class ThrowingConnectionFactory : KeranjangHijau.Core.Interfaces.IDbConnectionFactory
    {
        public Task<System.Data.Common.DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Database tidak boleh disentuh di test ini.");
    }
}
