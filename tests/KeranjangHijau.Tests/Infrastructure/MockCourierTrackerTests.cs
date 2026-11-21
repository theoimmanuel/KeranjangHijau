using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Infrastructure.Tracking;

namespace KeranjangHijau.Tests.Infrastructure;

public sealed class MockCourierTrackerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"kh-mock-{Guid.NewGuid():N}");

    public MockCourierTrackerTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    // ---------------------------------------------------------------- File asli (disalin ke output)

    [Fact]
    public void DefaultFile_IsCopiedToOutput()
    {
        Assert.True(File.Exists(new MockCourierTracker().FilePath));
    }

    [Fact]
    public async Task Track_ExistingResi_ReturnsData()
    {
        var result = await new MockCourierTracker().TrackAsync("jne", "JNE1001000003");

        Assert.NotNull(result);
        Assert.Equal("JNE1001000003", result.NomorResi);
        Assert.Equal("jne", result.KodeKurir);
        Assert.Equal("Jakarta", result.KotaAsal);
        Assert.Equal("Medan", result.KotaTujuan);
        Assert.Equal("YES", result.Layanan);
        Assert.Equal(0.8m, result.BeratKg);
        Assert.Equal(TrackingSource.Mock, result.Sumber);
    }

    [Fact]
    public async Task Track_ResiWithoutWeightOrService_ReturnsNulls()
    {
        var result = await new MockCourierTracker().TrackAsync("jnt", "JP1002000004");

        Assert.NotNull(result);
        Assert.Null(result.BeratKg);
        Assert.Null(result.Layanan);
    }

    [Fact]
    public async Task Track_UnknownResi_ReturnsNull()
    {
        Assert.Null(await new MockCourierTracker().TrackAsync("jne", "TIDAKADA0000"));
    }

    [Fact]
    public async Task Track_ResiOfOtherCourier_ReturnsNull()
    {
        // JNE1001000001 milik JNE, bukan J&T.
        Assert.Null(await new MockCourierTracker().TrackAsync("jnt", "JNE1001000001"));
    }

    [Theory]
    [InlineData("JNE", "JNE1001000001")]
    [InlineData("jne", "jne1001000001")]
    [InlineData(" Jne ", "  jne 1001 000 001 ")]
    public async Task Track_IsCaseInsensitive_AndIgnoresSpaces(string kodeKurir, string nomorResi)
    {
        var result = await new MockCourierTracker().TrackAsync(kodeKurir, nomorResi);

        Assert.NotNull(result);
        Assert.Equal("JNE1001000001", result.NomorResi);
        Assert.Equal("jne", result.KodeKurir);
    }

    [Fact]
    public async Task Track_Cancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new MockCourierTracker().TrackAsync("jne", "JNE1001000001", cts.Token));
    }

    // ---------------------------------------------------------------- File sementara

    [Fact]
    public async Task Track_ReadsFileOnlyOnce()
    {
        var path = WriteFile("""
            [ { "kodeKurir": "pos", "nomorResi": "POS0000000001", "kotaAsal": "Jakarta", "kotaTujuan": "Bogor", "layanan": "Pos Reguler", "beratKg": 1 } ]
            """);
        var tracker = new MockCourierTracker(path);

        Assert.NotNull(await tracker.TrackAsync("pos", "POS0000000001"));
        File.Delete(path);

        Assert.NotNull(await tracker.TrackAsync("pos", "POS0000000001"));
    }

    [Fact]
    public async Task Track_MissingFile_ThrowsHelpfulMessage()
    {
        var tracker = new MockCourierTracker(Path.Combine(_tempDir, "tidak-ada.json"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tracker.TrackAsync("jne", "JNE1001000001"));

        Assert.Contains("tidak ditemukan", ex.Message);
    }

    [Theory]
    [InlineData("""[ { "kodeKurir": "jne", "nomorResi": "AB-1", "kotaAsal": "Jakarta", "kotaTujuan": "Bogor" } ]""", "tidak valid")]
    [InlineData("""[ { "kodeKurir": "jne", "nomorResi": "JNE00000001", "kotaAsal": "Jakarta" } ]""", "tidak lengkap")]
    [InlineData("""[ { "kodeKurir": "jne", "nomorResi": "JNE00000001", "kotaAsal": "Jakarta", "kotaTujuan": "Bogor", "beratKg": 0 } ]""", "Berat")]
    [InlineData("""
        [ { "kodeKurir": "jne", "nomorResi": "JNE00000001", "kotaAsal": "Jakarta", "kotaTujuan": "Bogor" },
          { "kodeKurir": "JNE", "nomorResi": "jne 00000001", "kotaAsal": "Bandung", "kotaTujuan": "Bogor" } ]
        """, "ganda")]
    [InlineData("{ bukan json", "Format")]
    public async Task Track_InvalidFile_Throws(string json, string expectedMessagePart)
    {
        var tracker = new MockCourierTracker(WriteFile(json));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tracker.TrackAsync("jne", "JNE00000001"));

        Assert.Contains(expectedMessagePart, ex.Message);
    }

    private string WriteFile(string json)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
