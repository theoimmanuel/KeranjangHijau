namespace KeranjangHijau.Tests.Fakes;

public class FakeClockTests
{
    [Fact]
    public void UtcNow_IsNormalizedToUtc_AndAdvancesManually()
    {
        var wib = TimeSpan.FromHours(7);
        var clock = new FakeClock(new DateTimeOffset(2025, 3, 3, 8, 0, 0, wib));

        clock.Advance(TimeSpan.FromDays(1));

        Assert.Equal(TimeSpan.Zero, clock.UtcNow.Offset);
        Assert.Equal(new DateTimeOffset(2025, 3, 4, 1, 0, 0, TimeSpan.Zero), clock.UtcNow);
    }
}
