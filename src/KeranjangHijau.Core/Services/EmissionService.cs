using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;

namespace KeranjangHijau.Core.Services;

/// <summary>
/// Implementasi <see cref="IEmissionService"/>. Urutan: cek sesi (role pengguna) → validasi resi →
/// validasi nomor HP opsional → tracker → cocokkan kota (<see cref="CityNameMatcher"/>) → berat,
/// jarak, moda, faktor, CO2e → simpan shipment + record + <see cref="IEmissionRecordedHandler"/>
/// dalam SATU transaksi.
/// </summary>
/// <remarks>
/// Pemanggilan tracker (bisa lambat, HTTP) dan semua perhitungan dilakukan <em>sebelum</em> transaksi
/// dibuka, supaya transaksi database sesingkat mungkin. Nomor HP hanya diteruskan ke tracker,
/// tidak pernah disimpan atau dimasukkan ke pesan error.
/// </remarks>
public sealed class EmissionService : IEmissionService
{
    public const string NotLoggedInMessage = "Silakan login terlebih dahulu.";
    public const string NotPenggunaMessage = "Hanya pengguna yang dapat menghitung emisi paket.";
    public const string ResiNotFoundMessage = "Data resi tidak ditemukan.";
    public const string KotaAsalField = "KotaAsal";
    public const string KotaTujuanField = "KotaTujuan";

    private readonly ICurrentUserSession _session;
    private readonly IResiValidator _resiValidator;
    private readonly ICourierTracker _tracker;
    private readonly ICityRepository _cities;
    private readonly IWeightResolver _weightResolver;
    private readonly IDistanceCalculator _distanceCalculator;
    private readonly ITransportModeResolver _modeResolver;
    private readonly IEmissionFactorRepository _factors;
    private readonly ICarbonCalculator _carbonCalculator;
    private readonly IShipmentRepository _shipments;
    private readonly IEmissionRecordRepository _records;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IClock _clock;
    private readonly IReadOnlyList<IEmissionRecordedHandler> _handlers;

    public EmissionService(
        ICurrentUserSession session,
        IResiValidator resiValidator,
        ICourierTracker tracker,
        ICityRepository cities,
        IWeightResolver weightResolver,
        IDistanceCalculator distanceCalculator,
        ITransportModeResolver modeResolver,
        IEmissionFactorRepository factors,
        ICarbonCalculator carbonCalculator,
        IShipmentRepository shipments,
        IEmissionRecordRepository records,
        ITransactionRunner transactionRunner,
        IClock clock,
        IEnumerable<IEmissionRecordedHandler> handlers)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _resiValidator = resiValidator ?? throw new ArgumentNullException(nameof(resiValidator));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _cities = cities ?? throw new ArgumentNullException(nameof(cities));
        _weightResolver = weightResolver ?? throw new ArgumentNullException(nameof(weightResolver));
        _distanceCalculator = distanceCalculator ?? throw new ArgumentNullException(nameof(distanceCalculator));
        _modeResolver = modeResolver ?? throw new ArgumentNullException(nameof(modeResolver));
        _factors = factors ?? throw new ArgumentNullException(nameof(factors));
        _carbonCalculator = carbonCalculator ?? throw new ArgumentNullException(nameof(carbonCalculator));
        _shipments = shipments ?? throw new ArgumentNullException(nameof(shipments));
        _records = records ?? throw new ArgumentNullException(nameof(records));
        _transactionRunner = transactionRunner ?? throw new ArgumentNullException(nameof(transactionRunner));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        ArgumentNullException.ThrowIfNull(handlers);
        _handlers = handlers.ToList();
    }

    public static string CityNotFoundMessage(string label, string namaKota) =>
        $"Kota {label} \"{namaKota}\" belum terdaftar di sistem.";

    public async Task<EmissionResultDto> CalculateFromResiAsync(
        EmissionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // a. Sesi: hanya pengguna (bukan admin) yang boleh menghitung.
        var user = EnsurePengguna();

        // b. Format resi → kurir → duplikat (CLAUDE.md 7.6).
        var validated = await _resiValidator
            .ValidateAsync(request.KodeKurir, request.NomorResi, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var courier = validated.Courier;

        // b2. Nomor HP opsional (M2.4): kosong = tidak dikirim; jika diisi harus tepat 5 digit.
        if (PhoneSuffixRule.GetError(request.NomorHpAkhir) is { } hpError)
        {
            throw new ValidationException(hpError, PhoneSuffixRule.Field);
        }

        // c. Data paket dari tracker (API → mock).
        var trackingRequest = new TrackingRequest(
            courier.KodeKurir, validated.NomorResi, PhoneSuffixRule.Normalize(request.NomorHpAkhir));
        var tracking = await _tracker.TrackAsync(trackingRequest, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(ResiNotFoundMessage);

        // d. Cocokkan kota ke tabel cities (toleran terhadap format nama dari API, M2.4).
        var cities = await _cities.GetAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var origin = FindCity(tracking.KotaAsal, cities, "asal", KotaAsalField);
        var destination = FindCity(tracking.KotaTujuan, cities, "tujuan", KotaTujuanField);

        // e. Berat, jarak, moda, faktor, CO2e.
        var layanan = NormalizeLayanan(tracking.Layanan);
        var weight = _weightResolver.Resolve(tracking.BeratKg, request.BeratManualKg);
        var jarakKm = _distanceCalculator.CalculateDistanceKm(origin, destination);
        var mode = _modeResolver.Resolve(origin.Pulau, destination.Pulau, layanan);
        var factor = await _factors.GetByModeAsync(mode, cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Faktor emisi untuk moda {mode.ToDbValue()} belum tersedia.");
        var carbon = _carbonCalculator.Calculate(weight.BeratKg, jarakKm, factor);

        var now = _clock.UtcNow;
        var shipment = new Shipment
        {
            UserId = user.UserId,
            CourierId = courier.CourierId,
            NomorResi = validated.NomorResi,
            OriginCityId = origin.CityId,
            DestCityId = destination.CityId,
            Layanan = layanan,
            BeratKg = weight.BeratKg,
            SumberData = ShipmentDataSourceExtensions.From(tracking.Sumber, weight.Sumber),
            CreatedAt = now,
        };
        var record = new EmissionRecord
        {
            FactorId = factor.FactorId,
            JarakKm = jarakKm,
            TotalCo2eKg = carbon.TotalCo2eKg,
            Metode = carbon.Metode,
            CalculatedAt = now,
        };

        // f. Simpan dalam SATU transaksi, termasuk handler lanjutan (M4.1, M5.1).
        return await _transactionRunner.ExecuteAsync(
            async (tx, ct) =>
            {
                shipment.ShipmentId = await _shipments.InsertAsync(shipment, tx, ct).ConfigureAwait(false);
                record.ShipmentId = shipment.ShipmentId;
                record.RecordId = await _records.InsertAsync(record, tx, ct).ConfigureAwait(false);

                // g. Hasil untuk UI.
                var result = new EmissionResultDto(
                    record.RecordId,
                    shipment.ShipmentId,
                    shipment.NomorResi,
                    courier.KodeKurir,
                    courier.NamaKurir,
                    origin.NamaKota,
                    destination.NamaKota,
                    layanan,
                    weight.BeratKg,
                    weight.Sumber,
                    shipment.SumberData,
                    jarakKm,
                    mode,
                    carbon.TotalCo2eKg,
                    carbon.Metode,
                    now);

                var context = new EmissionRecordedContext(user, shipment, record, result);
                foreach (var handler in _handlers)
                {
                    await handler.HandleAsync(context, tx, ct).ConfigureAwait(false);
                }

                // Handler boleh memperkaya hasil (mis. konversi setara M4.1).
                return context.Result;
            },
            cancellationToken).ConfigureAwait(false);
    }

    private UserDto EnsurePengguna()
    {
        var user = _session.CurrentUser ?? throw new UnauthorizedException(NotLoggedInMessage);
        if (user.Role != UserRole.Pengguna)
        {
            throw new UnauthorizedException(NotPenggunaMessage);
        }

        return user;
    }

    private static City FindCity(string? namaKota, IReadOnlyList<City> cities, string label, string field)
    {
        var name = namaKota?.Trim() ?? string.Empty;
        return CityNameMatcher.Match(name, cities)
            ?? throw new ValidationException(CityNotFoundMessage(label, name), field);
    }

    // Layanan kosong → null (dianggap reguler); dipotong agar muat di shipments.layanan VARCHAR(50).
    private static string? NormalizeLayanan(string? layanan)
    {
        if (string.IsNullOrWhiteSpace(layanan))
        {
            return null;
        }

        var trimmed = layanan.Trim();
        return trimmed.Length <= Shipment.MaxLayananLength ? trimmed : trimmed[..Shipment.MaxLayananLength].TrimEnd();
    }
}
