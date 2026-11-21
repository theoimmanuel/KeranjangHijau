using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Tests.Fakes;

/// <summary><see cref="ICourierTracker"/> yang mengembalikan data yang didaftarkan lewat <see cref="Add"/>.</summary>
public sealed class FakeCourierTracker : ICourierTracker
{
    private readonly Dictionary<(string KodeKurir, string NomorResi), TrackingResult> _results = [];

    public int CallCount { get; private set; }

    /// <summary>Permintaan terakhir apa adanya (belum dinormalisasi).</summary>
    public TrackingRequest? LastRequest { get; private set; }

    public void Add(TrackingResult result) =>
        _results[(result.KodeKurir.ToLowerInvariant(), ResiNormalizer.Normalize(result.NomorResi))] = result;

    public Task<TrackingResult?> TrackAsync(TrackingRequest request, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastRequest = request;
        return Task.FromResult(_results.GetValueOrDefault((request.KodeKurir.Trim().ToLowerInvariant(), ResiNormalizer.Normalize(request.NomorResi))));
    }
}
