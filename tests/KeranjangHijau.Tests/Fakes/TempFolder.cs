namespace KeranjangHijau.Tests.Fakes;

/// <summary>Folder sementara unik untuk test file; dihapus beserta isinya saat dispose.</summary>
public sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"kh-test-{Guid.NewGuid():N}");
    }

    /// <summary>Path folder (belum dibuat sampai ada yang membuatnya).</summary>
    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
