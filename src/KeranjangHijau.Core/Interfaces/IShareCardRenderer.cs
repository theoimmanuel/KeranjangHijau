using KeranjangHijau.Core.Dtos;

namespace KeranjangHijau.Core.Interfaces;

/// <summary>
/// Menggambar kartu Instagram Story (PNG <see cref="ShareCardData.LebarPx"/> × <see cref="ShareCardData.TinggiPx"/>)
/// dari <see cref="ShareCardData"/> (CLAUDE.md 7.9). Implementasi sebenarnya (<c>WpfShareCardRenderer</c>) dibuat
/// tim frontend di proyek WPF dan didaftarkan sebagai <c>IShareCardRenderer</c> di DI.
/// </summary>
public interface IShareCardRenderer
{
    /// <summary>
    /// Menulis PNG ke <paramref name="outputPath"/> (path lengkap; foldernya sudah dibuat oleh
    /// <see cref="IShareService"/>) dan mengembalikan path file yang benar-benar ditulis.
    /// </summary>
    Task<string> RenderAsync(ShareCardData data, string outputPath, CancellationToken cancellationToken = default);
}
