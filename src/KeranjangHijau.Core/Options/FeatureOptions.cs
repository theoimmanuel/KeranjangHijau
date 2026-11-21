namespace KeranjangHijau.Core.Options;

/// <summary>
/// Saklar fitur (bagian <c>Features</c> di konfigurasi).
/// </summary>
public sealed class FeatureOptions
{
    public const string SectionName = "Features";

    /// <summary>
    /// Jika <c>true</c> (mode development), resi yang sama untuk kurir yang sama boleh
    /// diinput berkali-kali. Ubah menjadi <c>false</c> sebelum rilis (CLAUDE.md 7.6).
    /// </summary>
    public bool AllowDuplicateResi { get; set; } = true;
}
