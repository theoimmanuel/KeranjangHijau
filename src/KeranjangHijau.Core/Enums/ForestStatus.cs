namespace KeranjangHijau.Core.Enums;

/// <summary>Status hutan virtual (kolom <c>forests.status</c>, CLAUDE.md 7.7), dari paling sehat ke paling rusak.</summary>
/// <remarks>
/// Di database disimpan sebagai teks yang sama persis dengan CHECK <c>ck_forests_status</c>.
/// Konversi selalu lewat <see cref="ForestStatusExtensions"/>, jangan memakai <c>ToString()</c>.
/// </remarks>
public enum ForestStatus
{
    Lestari,
    Sehat,
    Menipis,
    Terancam,
    Kritis,
}

/// <summary>Konversi <see cref="ForestStatus"/> dari/ke teks yang disimpan di database.</summary>
public static class ForestStatusExtensions
{
    public const string LestariValue = "Lestari";
    public const string SehatValue = "Sehat";
    public const string MenipisValue = "Menipis";
    public const string TerancamValue = "Terancam";
    public const string KritisValue = "Kritis";

    /// <summary>Teks untuk kolom <c>forests.status</c>.</summary>
    public static string ToDbValue(this ForestStatus status) => status switch
    {
        ForestStatus.Lestari => LestariValue,
        ForestStatus.Sehat => SehatValue,
        ForestStatus.Menipis => MenipisValue,
        ForestStatus.Terancam => TerancamValue,
        ForestStatus.Kritis => KritisValue,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status hutan tidak dikenal."),
    };

    /// <summary>
    /// Mengubah teks dari database ke <see cref="ForestStatus"/>. Tidak peka huruf besar/kecil
    /// dan mengabaikan spasi di awal/akhir.
    /// </summary>
    /// <exception cref="ArgumentException">Teks bukan salah satu dari 5 status.</exception>
    public static ForestStatus ParseForestStatus(string value) =>
        TryParseForestStatus(value, out var status)
            ? status
            : throw new ArgumentException($"Status hutan '{value}' tidak dikenal.", nameof(value));

    public static bool TryParseForestStatus(string? value, out ForestStatus status)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "lestari":
                status = ForestStatus.Lestari;
                return true;
            case "sehat":
                status = ForestStatus.Sehat;
                return true;
            case "menipis":
                status = ForestStatus.Menipis;
                return true;
            case "terancam":
                status = ForestStatus.Terancam;
                return true;
            case "kritis":
                status = ForestStatus.Kritis;
                return true;
            default:
                status = default;
                return false;
        }
    }
}
