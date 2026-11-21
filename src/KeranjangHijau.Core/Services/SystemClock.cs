using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Core.Services;

/// <summary>Implementasi <see cref="IClock"/> yang memakai jam sistem (UTC).</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
