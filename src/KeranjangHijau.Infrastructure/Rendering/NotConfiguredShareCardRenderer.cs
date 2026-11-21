using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Infrastructure.Rendering;

/// <summary>
/// <see cref="IShareCardRenderer"/> bawaan backend selama proyek WPF belum mendaftarkan renderer sendiri.
/// Selalu gagal dengan pesan jelas; didaftarkan dengan <c>TryAdd</c>, jadi renderer WPF yang didaftarkan
/// sebelum atau sesudah <c>AddKeranjangHijauBackend</c> tetap dipakai.
/// </summary>
public sealed class NotConfiguredShareCardRenderer : IShareCardRenderer
{
    public const string NotConfiguredMessage =
        "Renderer kartu bagikan belum dipasang. Daftarkan implementasi IShareCardRenderer dari proyek WPF.";

    public Task<string> RenderAsync(ShareCardData data, string outputPath, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(NotConfiguredMessage);
}
