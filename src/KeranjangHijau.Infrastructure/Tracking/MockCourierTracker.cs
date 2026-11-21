using System.Text.Json;
using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Infrastructure.Tracking;

/// <summary>
/// Tracker cadangan yang membaca <c>MockData/mock-resi.json</c> (CLAUDE.md 7.10).
/// File dibaca satu kali saat pemanggilan pertama, lalu disimpan di memori.
/// </summary>
public sealed class MockCourierTracker : ICourierTracker
{
    public const string DefaultRelativePath = "MockData/mock-resi.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _filePath;
    private readonly Lazy<Task<IReadOnlyDictionary<(string KodeKurir, string NomorResi), TrackingResult>>> _data;

    /// <param name="filePath">
    /// Lokasi file JSON. Default: <see cref="DefaultRelativePath"/> di folder aplikasi
    /// (file disalin ke output saat build).
    /// </param>
    public MockCourierTracker(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, DefaultRelativePath);
        _data = new(LoadAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Lokasi file yang dibaca tracker ini.</summary>
    public string FilePath => _filePath;

    /// <remarks><see cref="TrackingRequest.NomorHpAkhir"/> diabaikan (data mock tidak memerlukannya).</remarks>
    public async Task<TrackingResult?> TrackAsync(TrackingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var normalized = request.Normalize();
        var data = await _data.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
        return data.GetValueOrDefault((normalized.KodeKurir, normalized.NomorResi));
    }

    private async Task<IReadOnlyDictionary<(string, string), TrackingResult>> LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            throw new InvalidOperationException($"File data resi mock tidak ditemukan: {_filePath}");
        }

        List<MockResiEntry>? entries;
        await using (var stream = File.OpenRead(_filePath))
        {
            try
            {
                entries = await JsonSerializer.DeserializeAsync<List<MockResiEntry>>(stream, JsonOptions).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Format file data resi mock tidak valid ({_filePath}): {ex.Message}", ex);
            }
        }

        var result = new Dictionary<(string, string), TrackingResult>();
        foreach (var (entry, index) in (entries ?? []).Select((e, i) => (e, i)))
        {
            var item = entry.ToResult(index);
            if (!result.TryAdd((item.KodeKurir, item.NomorResi), item))
            {
                throw new InvalidOperationException(
                    $"Data resi mock ganda: {item.KodeKurir} {item.NomorResi} (entri ke-{index + 1}).");
            }
        }

        return result;
    }

    /// <summary>Bentuk satu entri di file JSON. Field lain (mis. <c>skenario</c>) diabaikan.</summary>
    private sealed class MockResiEntry
    {
        public string? KodeKurir { get; init; }

        public string? NomorResi { get; init; }

        public string? KotaAsal { get; init; }

        public string? KotaTujuan { get; init; }

        public string? Layanan { get; init; }

        public decimal? BeratKg { get; init; }

        public TrackingResult ToResult(int index)
        {
            var position = $"entri ke-{index + 1}";
            if (string.IsNullOrWhiteSpace(KodeKurir) || string.IsNullOrWhiteSpace(NomorResi)
                || string.IsNullOrWhiteSpace(KotaAsal) || string.IsNullOrWhiteSpace(KotaTujuan))
            {
                throw new InvalidOperationException(
                    $"Data resi mock tidak lengkap ({position}): kodeKurir, nomorResi, kotaAsal, dan kotaTujuan wajib diisi.");
            }

            var resi = ResiNormalizer.Normalize(NomorResi);
            if (ResiFormatRule.GetError(resi) is { } formatError)
            {
                throw new InvalidOperationException($"Nomor resi mock tidak valid ({position}): {formatError}");
            }

            if (BeratKg is <= 0)
            {
                throw new InvalidOperationException($"Berat resi mock harus lebih dari 0 ({position}).");
            }

            return new TrackingResult(
                resi,
                KodeKurir.Trim().ToLowerInvariant(),
                KotaAsal.Trim(),
                KotaTujuan.Trim(),
                string.IsNullOrWhiteSpace(Layanan) ? null : Layanan.Trim(),
                BeratKg,
                TrackingSource.Mock);
        }
    }
}
