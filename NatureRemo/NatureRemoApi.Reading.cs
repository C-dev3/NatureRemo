namespace NatureRemo;

// Read-only endpoints that the Python library does not provide.
// Requires `public partial class NatureRemoApi` in NatureRemoApi.cs.
public partial class NatureRemoApi
{
    /// <summary>GET a JSON object and deserialize it.</summary>
    private async Task<T> FetchAsync<T>(string endpoint, CancellationToken ct)
    {
        var resp = await RequestAsync(endpoint, HttpMethod.Get, null, ct).ConfigureAwait(false);
        return await GetJsonAsync<T>(resp, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// GET a JSON array and deserialize it. The API may return <c>null</c> instead of
    /// <c>[]</c> for an empty list (the specification marks these arrays as nullable),
    /// so <c>null</c> is mapped to an empty list here.
    /// </summary>
    private async Task<List<T>> FetchListAsync<T>(string endpoint, CancellationToken ct)
    {
        var resp = await RequestAsync(endpoint, HttpMethod.Get, null, ct).ConfigureAwait(false);
        return await GetJsonListAsync<T>(resp, ct).ConfigureAwait(false);
    }

    /// <summary>Fetch the list of appliances registered to a Remo device.</summary>
    /// <param name="device">Device ID.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<List<Appliance>> GetDeviceAppliancesAsync(string device, CancellationToken ct = default) => FetchListAsync<Appliance>($"/1/devices/{device}/appliances", ct);

    /// <summary>Fetch the list of homes.</summary>
    /// <param name="ct">Cancellation token.</param>
    public Task<List<Home>> GetHomesAsync(CancellationToken ct = default) => FetchListAsync<Home>("/1/homes", ct);

    /// <summary>Fetch the list of devices in a home.</summary>
    /// <param name="home">Home ID.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<List<Device>> GetHomeDevicesAsync(string home, CancellationToken ct = default) => FetchListAsync<Device>($"/1/homes/{home}/devices", ct);

    /// <summary>Fetch the list of users in a home, with their roles.</summary>
    /// <param name="home">Home ID.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<List<HomeUserRole>> GetHomeUsersAsync(string home, CancellationToken ct = default) => FetchListAsync<HomeUserRole>($"/1/homes/{home}/users", ct);

    /// <summary>
    /// Fetch hourly energy data for the appliances registered to a device
    /// (requires the "electricity" scope). Each metric is a series of
    /// [unix time in seconds, kWh] pairs; use <see cref="DeviceEnergyTimeSeries.ToPoints"/>
    /// to convert a series to <see cref="EnergyPoint"/> values.
    /// </summary>
    /// <param name="device">Device ID.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<DeviceEnergyTimeSeries> GetHourlyElectricityAsync(string device, CancellationToken ct = default) => FetchAsync<DeviceEnergyTimeSeries>($"/1/devices/{device}/electricity/time_series/hourly", ct);

    /// <summary>Fetch the list of ECHONET Lite appliances (for example those seen by Nature Remo E).</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<EchonetLiteAppliance>> GetEchonetLiteAppliancesAsync(CancellationToken ct = default)
    {
        var response = await FetchAsync<EchonetLiteApplianceResponse>("/1/echonetlite/appliances", ct).ConfigureAwait(false);
        return response.Appliances ?? [];
    }
}
