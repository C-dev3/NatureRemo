using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace NatureRemo;

/// <summary>Rate limit information taken from the latest API response.</summary>
public class RateLimit
{
    /// <summary>Time (UTC) of the response these values were taken from.</summary>
    public DateTime? CheckedAt { get; set; }

    /// <summary>Maximum number of requests allowed in the current window.</summary>
    public int? Limit { get; set; }

    /// <summary>Number of requests remaining in the current window.</summary>
    public int? Remaining { get; set; }

    /// <summary>Time (UTC) when the rate limit window resets.</summary>
    public DateTime? Reset { get; set; }
}

/// <summary>Client for the Nature Remo cloud API.</summary>
/// <remarks>Creates a new cloud API client.</remarks>
/// <param name="accessToken">Nature Remo access token.</param>
/// <param name="httpClient">
/// Optional <see cref="HttpClient"/>. If omitted, one is created and disposed by this instance.
/// </param>
public partial class NatureRemoApi(string accessToken, HttpClient? httpClient = null) : IDisposable
{
    /// <summary>Base URL of the Nature Remo cloud API.</summary>
    public const string BaseUrl = "https://api.nature.global";
    private const string Version = "0.3.3";
    private const string ProjectUrl = "https://github.com/morinokami/nature-remo";

    private readonly HttpClient _http = httpClient ?? new HttpClient();
    private readonly bool _ownsClient = httpClient is null;

    /// <summary>Access token used for the Authorization header.</summary>
    public string AccessToken { get; } = accessToken;

    /// <summary>Rate limit information taken from the most recent response.</summary>
    public RateLimit RateLimit { get; } = new();

    /// <summary>Disposes the internal <see cref="HttpClient"/> if this instance created it.</summary>
    public void Dispose()
    {
        if (_ownsClient) _http.Dispose();
        GC.SuppressFinalize(this);
    }

    // ---- internal helpers -------------------------------------------------

    private async Task<HttpResponseMessage> RequestAsync(
        string endpoint,
        HttpMethod method,
        IEnumerable<KeyValuePair<string, string>>? data = null,
        CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(method, BaseUrl + endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        request.Headers.UserAgent.ParseAdd($"nature-remo/{Version} ({ProjectUrl})");

        if (method == HttpMethod.Post)
            request.Content = new FormUrlEncodedContent(data ?? []);

        try
        {
            return await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            throw new NatureRemoException(e.Message, e);
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> Form(params (string Key, string? Value)[] items) =>
        items.Where(i => !string.IsNullOrEmpty(i.Value))
             .Select(i => new KeyValuePair<string, string>(i.Key, i.Value!));

    private void SetRateLimit(HttpResponseMessage resp)
    {
        if (resp.Headers.Date is { } date)
            RateLimit.CheckedAt = date.UtcDateTime;

        if (TryGetHeader(resp, "X-Rate-Limit-Limit", out var limit) &&
            int.TryParse(limit, out var l))
            RateLimit.Limit = l;

        if (TryGetHeader(resp, "X-Rate-Limit-Remaining", out var remaining) &&
            int.TryParse(remaining, out var r))
            RateLimit.Remaining = r;

        if (TryGetHeader(resp, "X-Rate-Limit-Reset", out var reset) &&
            long.TryParse(reset, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ts))
            RateLimit.Reset = DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;
    }

    private static bool TryGetHeader(HttpResponseMessage resp, string name, out string value)
    {
        if (resp.Headers.TryGetValues(name, out var values))
        {
            value = values.FirstOrDefault() ?? "";
            return true;
        }
        value = "";
        return false;
    }

    private static async Task<string> BuildErrorMessageAsync(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var code = root.TryGetProperty("code", out var c) ? c.ToString() : "";
            var message = root.TryGetProperty("message", out var m) ? m.GetString() : "";
            return $"HTTP Status Code: {(int)resp.StatusCode}, Nature Remo Code: {code}, Message: {message}";
        }
        catch (JsonException)
        {
            return $"HTTP Status Code: {(int)resp.StatusCode}, Body: {body}";
        }
    }

    private async Task<T> GetJsonAsync<T>(HttpResponseMessage resp, CancellationToken ct)
    {
        using (resp)
        {
            SetRateLimit(resp);
            if (!resp.IsSuccessStatusCode)
                throw new NatureRemoException(await BuildErrorMessageAsync(resp).ConfigureAwait(false));

            var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var result = await JsonSerializer.DeserializeAsync<T>(stream, Json.Options, ct).ConfigureAwait(false);
            return result ?? throw new NatureRemoException("Empty response body");
        }
    }

    private async Task<List<T>> GetJsonListAsync<T>(HttpResponseMessage resp, CancellationToken ct)
    {
        using (resp)
        {
            SetRateLimit(resp);
            if (!resp.IsSuccessStatusCode)
                throw new NatureRemoException(await BuildErrorMessageAsync(resp).ConfigureAwait(false));

            var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var result = await JsonSerializer.DeserializeAsync<List<T>>(stream, Json.Options, ct)
                .ConfigureAwait(false);
            return result ?? [];
        }
    }

    private async Task<List<T>> PostJsonListAsync<T>(
        string endpoint, IEnumerable<KeyValuePair<string, string>>? data, CancellationToken ct)
    {
        var resp = await RequestAsync(endpoint, HttpMethod.Post, data, ct).ConfigureAwait(false);
        return await GetJsonListAsync<T>(resp, ct).ConfigureAwait(false);
    }

    private async Task EnsureOkAsync(HttpResponseMessage resp)
    {
        using (resp)
        {
            SetRateLimit(resp);
            if (!resp.IsSuccessStatusCode)
                throw new NatureRemoException(await BuildErrorMessageAsync(resp).ConfigureAwait(false));
        }
    }

    private async Task<T> PostJsonAsync<T>(
        string endpoint, IEnumerable<KeyValuePair<string, string>>? data, CancellationToken ct)
    {
        var resp = await RequestAsync(endpoint, HttpMethod.Post, data, ct).ConfigureAwait(false);
        return await GetJsonAsync<T>(resp, ct).ConfigureAwait(false);
    }

    private async Task PostAsync(
        string endpoint, IEnumerable<KeyValuePair<string, string>>? data, CancellationToken ct)
    {
        var resp = await RequestAsync(endpoint, HttpMethod.Post, data, ct).ConfigureAwait(false);
        await EnsureOkAsync(resp).ConfigureAwait(false);
    }

    // ---- user -------------------------------------------------------------

    /// <summary>Fetch the authenticated user's information.</summary>
    public async Task<User> GetUserAsync(CancellationToken ct = default)
    {
        var resp = await RequestAsync("/1/users/me", HttpMethod.Get, null, ct).ConfigureAwait(false);
        return await GetJsonAsync<User>(resp, ct).ConfigureAwait(false);
    }

    /// <summary>Update authenticated user's information.</summary>
    /// <param name="nickname">User's nickname.</param>
    /// <param name="country">Country (optional).</param>
    /// <param name="distanceUnit">Distance unit (optional).</param>
    /// <param name="tempUnit">Temperature unit, "c" or "f" (optional).</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<User> UpdateUserAsync(
        string nickname,
        string? country = null,
        string? distanceUnit = null,
        string? tempUnit = null,
        CancellationToken ct = default) => PostJsonAsync<User>("/1/users/me", Form(("nickname", nickname), ("country", country), ("distance_unit", distanceUnit), ("temp_unit", tempUnit)), ct);

    // ---- devices ----------------------------------------------------------

    /// <summary>Fetch the list of Remo devices the user has access to.</summary>
    public Task<List<Device>> GetDevicesAsync(CancellationToken ct = default) => FetchListAsync<Device>("/1/devices", ct);

    /// <summary>Update Remo.</summary>
    public Task<DeviceCore> UpdateDeviceAsync(string device, string name, CancellationToken ct = default) => PostJsonAsync<DeviceCore>($"/1/devices/{device}", Form(("name", name)), ct);

    /// <summary>Delete Remo.</summary>
    public Task DeleteDeviceAsync(string device, CancellationToken ct = default) => PostAsync($"/1/devices/{device}/delete", null, ct);

    /// <summary>Update temperature offset.</summary>
    public Task<Device> UpdateTemperatureOffsetAsync(string device, double offset, CancellationToken ct = default) => PostJsonAsync<Device>($"/1/devices/{device}/temperature_offset", Form(("offset", offset.ToString(CultureInfo.InvariantCulture))), ct);

    /// <summary>Update humidity offset.</summary>
    public Task<Device> UpdateHumidityOffsetAsync(string device, double offset, CancellationToken ct = default) => PostJsonAsync<Device>($"/1/devices/{device}/humidity_offset", Form(("offset", offset.ToString(CultureInfo.InvariantCulture))), ct);

    // ---- appliances -------------------------------------------------------

    /// <summary>
    /// Find the air conditioner best matching the provided infrared signal.
    /// <paramref name="message"/> is a JSON string with "data", "freq" and "format" keys.
    /// </summary>
    public Task<List<ApplianceModelAndParams>> DetectApplianceAsync(
    string message, string? device = null, CancellationToken ct = default) => PostJsonListAsync<ApplianceModelAndParams>("/1/detectappliance", Form(("message", message), ("device", device)), ct);

    /// <summary>Fetch the list of appliances.</summary>
    public Task<List<Appliance>> GetAppliancesAsync(CancellationToken ct = default) => FetchListAsync<Appliance>("/1/appliances", ct);

    /// <summary>Create a new appliance.</summary>
    /// <param name="device">Device ID.</param>
    /// <param name="nickname">Appliance name.</param>
    /// <param name="image">Basename of the image file included in the app.</param>
    /// <param name="model">ApplianceModel ID if the appliance is included in IRDB.</param>
    /// <param name="modelType">Type of model.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<Appliance> CreateApplianceAsync(
        string device,
        string nickname,
        string image,
        string? model = null,
        string? modelType = null,
        CancellationToken ct = default) => PostJsonAsync<Appliance>("/1/appliances", Form(("device", device), ("nickname", nickname), ("image", image), ("model", model), ("model_type", modelType)), ct);

    /// <summary>Reorder appliances. <paramref name="appliances"/> is a comma separated list of IDs.</summary>
    public Task UpdateApplianceOrdersAsync(string appliances, CancellationToken ct = default) =>
        PostAsync("/1/appliance_orders", Form(("appliances", appliances)), ct);

    /// <summary>Delete appliance.</summary>
    public Task DeleteApplianceAsync(string appliance, CancellationToken ct = default) =>
        PostAsync($"/1/appliances/{appliance}/delete", null, ct);

    /// <summary>Update appliance.</summary>
    public Task<Appliance> UpdateApplianceAsync(
        string appliance, string nickname, string image, CancellationToken ct = default) => PostJsonAsync<Appliance>($"/1/appliances/{appliance}", Form(("nickname", nickname), ("image", image)), ct);

    /// <summary>Update air conditioner settings. Only non-empty values are sent.</summary>
    public async Task<AirConParams> UpdateAirconSettingsAsync(
    string appliance,
    string? operationMode = null,
    string? temperature = null,
    string? airVolume = null,
    string? airDirection = null,
    string? button = null,
    string? airDirectionH = null,
    string? temperatureUnit = null,
    IReadOnlyDictionary<string, string>? extra = null,
    CancellationToken ct = default)
    {
        var data = Form(
            ("operation_mode", operationMode), ("temperature", temperature),
            ("air_volume", airVolume), ("air_direction", airDirection),
            ("button", button), ("air_direction_h", airDirectionH),
            ("temperature_unit", temperatureUnit)).ToList();

        if (extra is not null)
        {
            foreach (var (id, value) in extra)
                data.Add(new KeyValuePair<string, string>($"extra.{id}", value));
        }

        return await PostJsonAsync<AirConParams>(
            $"/1/appliances/{appliance}/aircon_settings", data, ct).ConfigureAwait(false);
    }

    /// <summary>Send tv infrared signal.</summary>
    public Task<TVState> SendTvInfraredSignalAsync(string appliance, string button, CancellationToken ct = default) => PostJsonAsync<TVState>($"/1/appliances/{appliance}/tv", Form(("button", button)), ct);

    /// <summary>Send light infrared signal.</summary>
    public Task<LightState> SendLightInfraredSignalAsync(string appliance, string button, CancellationToken ct = default) => PostJsonAsync<LightState>($"/1/appliances/{appliance}/light", Form(("button", button)), ct);

    // ---- signals ----------------------------------------------------------

    /// <summary>Fetch signals registered under this appliance.</summary>
    public Task<List<Signal>> GetSignalsAsync(string appliance, CancellationToken ct = default) => FetchListAsync<Signal>($"/1/appliances/{appliance}/signals", ct);

    /// <summary>Create a signal under this appliance.</summary>
    /// <param name="appliance">Appliance ID.</param>
    /// <param name="name">Signal name.</param>
    /// <param name="message">JSON string with "data", "freq" and "format" keys.</param>
    /// <param name="image">Basename of the image file included in the app.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<Signal> CreateSignalAsync(
        string appliance, string name, string message, string image, CancellationToken ct = default) => PostJsonAsync<Signal>($"/1/appliances/{appliance}/signals", Form(("name", name), ("message", message), ("image", image)), ct);

    /// <summary>Reorder signals under this appliance. <paramref name="signals"/> is a comma separated list of IDs.</summary>
    public Task UpdateSignalOrdersAsync(string appliance, string signals, CancellationToken ct = default) => PostAsync($"/1/appliances/{appliance}/signal_orders", Form(("signals", signals)), ct);

    /// <summary>Update infrared signal.</summary>
    public Task<Signal> UpdateSignalAsync(string signal, string name, string image, CancellationToken ct = default) => PostJsonAsync<Signal>($"/1/signals/{signal}", Form(("name", name), ("image", image)), ct);

    /// <summary>Delete infrared signal.</summary>
    public Task DeleteSignalAsync(string signal, CancellationToken ct = default) => PostAsync($"/1/signals/{signal}/delete", null, ct);

    /// <summary>Send infrared signal.</summary>
    public Task SendSignalAsync(string signal, CancellationToken ct = default) => PostAsync($"/1/signals/{signal}/send", null, ct);
}