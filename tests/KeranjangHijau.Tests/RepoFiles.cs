namespace KeranjangHijau.Tests;

/// <summary>Lokasi file di repo (untuk test teks terhadap file SQL/JSON, tanpa database).</summary>
internal static class RepoFiles
{
    public static string Root { get; } = FindRepoRoot();

    public static string Path(params string[] parts) => System.IO.Path.Combine([Root, .. parts]);

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(System.IO.Path.Combine(dir.FullName, "KeranjangHijau.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new DirectoryNotFoundException("Folder root repo (berisi KeranjangHijau.sln) tidak ditemukan.");
    }
}
