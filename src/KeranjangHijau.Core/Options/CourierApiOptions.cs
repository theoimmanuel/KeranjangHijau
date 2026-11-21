namespace KeranjangHijau.Core.Options;

/// <summary>
/// Pengaturan API cek resi (bagian <c>CourierApi</c> di konfigurasi, CLAUDE.md 7.10).
/// API key hanya boleh diisi di file <c>.env</c>, <c>appsettings.Local.json</c>, atau environment variable.
/// </summary>
public sealed class CourierApiOptions
{
    public const string SectionName = "CourierApi";

    /// <summary>Nama provider RajaOngkir (Komerce), API terpilih (M2.4).</summary>
    public const string RajaOngkirProvider = "RajaOngkir";

    /// <summary>Nama provider format netral contoh dari M2.3.</summary>
    public const string GenericProvider = "Generic";

    /// <summary>Kurir yang didukung endpoint cek resi RajaOngkir (dari pesan error API).</summary>
    public static IReadOnlyList<string> DefaultSupportedCouriers { get; } =
        ["jne", "jnt", "ninja", "tiki", "pos", "anteraja", "sap", "lion", "wahana", "first", "spx"];

    /// <summary>Adapter yang dipakai: <see cref="RajaOngkirProvider"/> (default) atau <see cref="GenericProvider"/>.</summary>
    public string Provider { get; set; } = RajaOngkirProvider;

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Batas waktu permintaan ke API (detik) sebelum beralih ke data mock.</summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Kode kurir yang dikirim ke API. Kurir lain langsung memakai data mock tanpa request, agar
    /// kuota API tidak terbuang. Jika konfigurasi tidak mengisi daftar ini, dipakai
    /// <see cref="DefaultSupportedCouriers"/>.
    /// </summary>
    /// <remarks>
    /// Sengaja kosong secara default: binder konfigurasi .NET <em>menambahkan</em> elemen ke
    /// koleksi yang sudah terisi, sehingga nilai default diisi lewat post-configure
    /// (pola sama dengan <see cref="EmissionOptions.ExpressKeywords"/>).
    /// </remarks>
    public List<string> SupportedCouriers { get; set; } = [];

    /// <summary><c>true</c> jika API key diisi. Jika kosong, tracker langsung memakai mock.</summary>
    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary><c>true</c> jika <see cref="Provider"/> berisi <see cref="GenericProvider"/> (tidak peka huruf besar/kecil).</summary>
    public bool UsesGenericProvider =>
        string.Equals(Provider?.Trim(), GenericProvider, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// <c>true</c> jika kurir boleh dikirim ke API (tidak peka huruf besar/kecil, spasi diabaikan).
    /// Daftar kosong berarti <see cref="DefaultSupportedCouriers"/>.
    /// </summary>
    public bool SupportsCourier(string? kodeKurir)
    {
        var kode = kodeKurir?.Trim();
        if (string.IsNullOrEmpty(kode))
        {
            return false;
        }

        var list = SupportedCouriers.Count > 0 ? (IReadOnlyList<string>)SupportedCouriers : DefaultSupportedCouriers;
        return list.Any(c => string.Equals(c?.Trim(), kode, StringComparison.OrdinalIgnoreCase));
    }
}
