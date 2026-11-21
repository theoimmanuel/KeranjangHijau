using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Tests.Fakes;

/// <summary><see cref="IClock"/> untuk test: waktunya diatur manual dan tidak berjalan sendiri.</summary>
public sealed class FakeClock : IClock
{
    private DateTimeOffset _utcNow;

    public FakeClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    /// <summary>Default: Senin, 3 Maret 2025 08.00 WIB (01.00 UTC).</summary>
    public FakeClock()
        : this(new DateTimeOffset(2025, 3, 3, 1, 0, 0, TimeSpan.Zero))
    {
    }

    public DateTimeOffset UtcNow
    {
        get => _utcNow;
        set => _utcNow = value.ToUniversalTime();
    }

    public void Advance(TimeSpan duration) => UtcNow = _utcNow + duration;
}
