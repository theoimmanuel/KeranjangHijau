using System.Collections;
using KeranjangHijau.Core.Options;
using Microsoft.Extensions.Configuration;

namespace KeranjangHijau.Infrastructure;

/// <summary>
/// Cara baku membaca konfigurasi backend. Urutan sumber (yang belakang menimpa yang depan):
/// <list type="number">
///   <item><c>appsettings.json</c>: nilai default, di-commit, TANPA rahasia.</item>
///   <item><c>appsettings.Local.json</c>: opsional, masuk <c>.gitignore</c>.</item>
///   <item>File <c>.env</c> (M2.4): opsional, masuk <c>.gitignore</c>, tempat API key dan rahasia lokal.
///         Hanya baris <c>KH_*</c> yang dibaca, dengan aturan pemetaan yang sama seperti environment variable.</item>
///   <item>Environment variable asli berawalan <c>KH_</c>, mis. <c>KH_Features__AllowDuplicateResi=false</c>.</item>
///   <item><c>KH_DB</c>: connection string database (prioritas tertinggi di lapisannya).</item>
/// </list>
/// Jadi environment variable asli selalu menang atas <c>.env</c>.
/// </summary>
/// <remarks>
/// Pemetaan <c>KH_*</c>: awalan dibuang dan <c>__</c> menjadi <c>:</c>
/// (<c>KH_CourierApi__ApiKey</c> → <c>CourierApi:ApiKey</c>). <c>KH_DB</c> dipetakan ke
/// <c>Database:ConnectionString</c>, baik dari <c>.env</c> maupun environment variable asli.
/// Nilai tidak pernah dimasukkan ke environment proses (tanpa <c>Environment.SetEnvironmentVariable</c>).
/// </remarks>
public static class BackendConfiguration
{
    public const string AppSettingsFile = "appsettings.json";
    public const string LocalSettingsFile = "appsettings.Local.json";
    public const string EnvironmentVariablePrefix = "KH_";
    public const string DbEnvironmentVariable = "KH_DB";

    /// <summary>Path file <c>.env</c> yang dipakai (menggantikan pencarian otomatis), mis. untuk test.</summary>
    public const string EnvFileVariable = "KH_ENV_FILE";

    private static readonly string ConnectionStringKey =
        $"{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.ConnectionString)}";

    /// <summary>
    /// Membangun konfigurasi dari folder aplikasi (default: <see cref="AppContext.BaseDirectory"/>).
    /// </summary>
    public static IConfigurationRoot Build(string? basePath = null) =>
        new ConfigurationBuilder()
            .AddKeranjangHijauSources(basePath ?? AppContext.BaseDirectory)
            .Build();

    /// <summary>
    /// Menambahkan semua sumber konfigurasi, termasuk <c>.env</c> yang dicari dari direktori kerja
    /// lalu dari <see cref="AppContext.BaseDirectory"/> (atau path di <c>KH_ENV_FILE</c>).
    /// </summary>
    public static IConfigurationBuilder AddKeranjangHijauSources(this IConfigurationBuilder builder, string basePath) =>
        builder.AddKeranjangHijauSources(
            basePath,
            ReadKhEnvironmentVariables(),
            [Directory.GetCurrentDirectory(), AppContext.BaseDirectory]);

    /// <summary>
    /// Menambahkan sumber konfigurasi dengan <c>KH_DB</c> tertentu dan <b>tanpa</b> file <c>.env</c>
    /// (untuk test yang tidak boleh terpengaruh <c>.env</c> milik pengembang).
    /// </summary>
    /// <param name="khDb">Nilai <c>KH_DB</c>; dipisah sebagai parameter agar bisa dites tanpa mengubah environment.</param>
    public static IConfigurationBuilder AddKeranjangHijauSources(
        this IConfigurationBuilder builder, string basePath, string? khDb)
    {
        var environment = ReadKhEnvironmentVariables();
        environment.Remove(DbEnvironmentVariable);
        environment.Remove(EnvFileVariable);
        if (khDb is not null)
        {
            environment[DbEnvironmentVariable] = khDb;
        }

        return builder.AddKeranjangHijauSources(basePath, environment, dotEnvSearchDirectories: []);
    }

    /// <summary>Bentuk lengkap; environment dan lokasi pencarian <c>.env</c> bisa diganti untuk test.</summary>
    /// <param name="environment">Environment variable (hanya kunci <c>KH_*</c> yang dipakai).</param>
    /// <param name="dotEnvSearchDirectories">
    /// Folder awal pencarian <c>.env</c>; diabaikan jika <c>KH_ENV_FILE</c> ada di <paramref name="environment"/>.
    /// </param>
    public static IConfigurationBuilder AddKeranjangHijauSources(
        this IConfigurationBuilder builder,
        string basePath,
        IReadOnlyDictionary<string, string?> environment,
        IEnumerable<string?> dotEnvSearchDirectories)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(dotEnvSearchDirectories);

        builder
            .AddJsonFile(Path.Combine(basePath, AppSettingsFile), optional: false, reloadOnChange: false)
            .AddJsonFile(Path.Combine(basePath, LocalSettingsFile), optional: true, reloadOnChange: false);

        if (ResolveDotEnvPath(environment, dotEnvSearchDirectories) is { } dotEnvPath)
        {
            var dotEnv = DotEnvFile.Read(dotEnvPath)
                .Select(p => new KeyValuePair<string, string?>(p.Key, p.Value));
            builder.AddInMemoryCollection(MapKhVariables(dotEnv));
        }

        builder.AddInMemoryCollection(MapKhVariables(environment));
        return builder;
    }

    /// <summary>
    /// Lokasi <c>.env</c> yang akan dibaca: <c>KH_ENV_FILE</c> jika diisi, selain itu hasil pencarian
    /// <see cref="DotEnvFile.Locate"/>. File tidak ada → <c>null</c> (bukan error).
    /// </summary>
    public static string? ResolveDotEnvPath(
        IReadOnlyDictionary<string, string?> environment, IEnumerable<string?> searchDirectories)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(searchDirectories);

        var explicitPath = Lookup(environment, EnvFileVariable);
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            var fullPath = Path.GetFullPath(explicitPath.Trim());
            return File.Exists(fullPath) ? fullPath : null;
        }

        return DotEnvFile.Locate(searchDirectories);
    }

    /// <summary>
    /// Memetakan variabel <c>KH_*</c> ke kunci konfigurasi: awalan dibuang, <c>__</c> menjadi <c>:</c>.
    /// <c>KH_DB</c> (jika tidak kosong) → <c>Database:ConnectionString</c>, menimpa kunci lain di lapisan
    /// yang sama. <c>KH_ENV_FILE</c> dan variabel tanpa awalan <c>KH_</c> diabaikan.
    /// </summary>
    public static IReadOnlyDictionary<string, string?> MapKhVariables(IEnumerable<KeyValuePair<string, string?>> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        string? khDb = null;
        foreach (var (key, value) in variables)
        {
            if (!key.StartsWith(EnvironmentVariablePrefix, StringComparison.OrdinalIgnoreCase)
                || key.Equals(EnvFileVariable, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (key.Equals(DbEnvironmentVariable, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    khDb = value;
                }

                continue;
            }

            var configKey = key[EnvironmentVariablePrefix.Length..].Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
            if (configKey.Length > 0)
            {
                result[configKey] = value;
            }
        }

        if (khDb is not null)
        {
            result[ConnectionStringKey] = khDb;
        }

        return result;
    }

    private static Dictionary<string, string?> ReadKhEnvironmentVariables()
    {
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is string key && key.StartsWith(EnvironmentVariablePrefix, StringComparison.OrdinalIgnoreCase))
            {
                result[key] = entry.Value as string;
            }
        }

        return result;
    }

    private static string? Lookup(IReadOnlyDictionary<string, string?> environment, string name) =>
        environment.TryGetValue(name, out var value)
            ? value
            : environment.FirstOrDefault(p => p.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;
}
