using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Tests.Fakes;

/// <summary>
/// <see cref="IShareCardRenderer"/> untuk test: mencatat panggilan dan menulis file kecil berisi tanda tangan PNG
/// (bukan gambar sungguhan) ke path yang diminta.
/// </summary>
public sealed class FakeShareCardRenderer : IShareCardRenderer
{
    /// <summary>8 byte awal file PNG.</summary>
    public static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public List<(ShareCardData Data, string OutputPath)> Calls { get; } = [];

    /// <summary>Jika diisi, <see cref="RenderAsync"/> melempar exception ini tanpa menulis file.</summary>
    public Exception? FailWith { get; set; }

    public async Task<string> RenderAsync(ShareCardData data, string outputPath, CancellationToken cancellationToken = default)
    {
        Calls.Add((data, outputPath));
        if (FailWith is not null)
        {
            throw FailWith;
        }

        await File.WriteAllBytesAsync(outputPath, PngSignature, cancellationToken);
        return outputPath;
    }
}
