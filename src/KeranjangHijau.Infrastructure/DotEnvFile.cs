namespace KeranjangHijau.Infrastructure;

/// <summary>
/// Pembaca file <c>.env</c> sederhana untuk konfigurasi backend (M2.4), tanpa paket tambahan.
/// </summary>
/// <remarks>
/// Format: <c>KEY=VALUE</c> per baris. Baris kosong dan baris berawalan <c>#</c> diabaikan, spasi
/// di sekitar kunci/nilai dibuang, dan tanda kutip pembungkus <c>"..."</c> atau <c>'...'</c> dilepas.
/// Hanya kunci berawalan <c>KH_</c> yang diambil (baris lain, mis. <c>POSTGRES_PORT</c> milik
/// docker compose, diabaikan). Tidak ada ekspansi variabel, komentar di akhir baris, atau nilai
/// multi-baris. Isi file tidak pernah dicatat ke log atau dimasukkan ke pesan error.
/// </remarks>
public static class DotEnvFile
{
    public const string FileName = ".env";

    /// <summary>
    /// Membaca pasangan kunci/nilai berawalan <c>KH_</c> (kunci apa adanya, mis.
    /// <c>KH_CourierApi__ApiKey</c>). Kunci yang sama muncul dua kali → nilai terakhir dipakai.
    /// File tidak ada → daftar kosong.
    /// </summary>
    public static IReadOnlyList<KeyValuePair<string, string>> Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            return [];
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Pesan exception asli hanya berisi path/izin, bukan isi file.
            throw new InvalidOperationException($"File {FileName} tidak bisa dibaca: {path}", ex);
        }

        return Parse(lines);
    }

    /// <summary>Mengurai isi file (per baris); lihat aturan format di ringkasan kelas.</summary>
    public static IReadOnlyList<KeyValuePair<string, string>> Parse(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var result = new List<KeyValuePair<string, string>>();
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            if (!key.StartsWith(BackendConfiguration.EnvironmentVariablePrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(new(key, Unquote(line[(separator + 1)..].Trim())));
        }

        return result;
    }

    /// <summary>
    /// Mencari <c>.env</c> mulai dari setiap folder di <paramref name="startDirectories"/> lalu naik
    /// ke folder induk. Pencarian dari satu folder awal berhenti di folder root repo (berisi
    /// <c>.git</c> atau <c>KeranjangHijau.sln</c>), setelah folder itu sendiri diperiksa.
    /// </summary>
    /// <returns>Path lengkap file pertama yang ditemukan, atau <c>null</c>.</returns>
    public static string? Locate(IEnumerable<string?> startDirectories)
    {
        ArgumentNullException.ThrowIfNull(startDirectories);

        foreach (var start in startDirectories)
        {
            if (string.IsNullOrWhiteSpace(start))
            {
                continue;
            }

            for (var dir = new DirectoryInfo(Path.GetFullPath(start)); dir is not null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, FileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                if (IsRepoRoot(dir))
                {
                    break;
                }
            }
        }

        return null;
    }

    private static bool IsRepoRoot(DirectoryInfo dir) =>
        Directory.Exists(Path.Combine(dir.FullName, ".git"))
        || File.Exists(Path.Combine(dir.FullName, ".git")) // worktree/submodule: .git berupa file
        || File.Exists(Path.Combine(dir.FullName, "KeranjangHijau.sln"));

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0])
        {
            return value[1..^1];
        }

        return value;
    }
}
