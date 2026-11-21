using KeranjangHijau.Infrastructure;

namespace KeranjangHijau.Tests.Integration;

/// <summary>
/// <see cref="FactAttribute"/> untuk smoke test ke API RajaOngkir sungguhan (memakai kuota).
/// Default selalu dilewati; hanya jalan jika env <c>KH_RAJAONGKIR_LIVE=1</c> dan API key terisi
/// (dari <c>.env</c>, <c>appsettings.Local.json</c>, atau environment variable).
/// </summary>
public sealed class RajaOngkirLiveFactAttribute : FactAttribute
{
    public const string LiveVariable = "KH_RAJAONGKIR_LIVE";

    public RajaOngkirLiveFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(LiveVariable) != "1")
        {
            Skip = $"{LiveVariable} bukan 1; smoke test API RajaOngkir dilewati (menghemat kuota).";
        }
        else if (string.IsNullOrWhiteSpace(BackendConfiguration.Build()["CourierApi:ApiKey"]))
        {
            Skip = "CourierApi:ApiKey kosong; smoke test API RajaOngkir dilewati.";
        }
    }
}
