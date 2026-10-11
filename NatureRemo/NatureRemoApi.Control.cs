using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NatureRemo;

// Control endpoints that the Python library does not provide.
// Requires `public partial class NatureRemoApi` in NatureRemoApi.cs.
//
// NOTE: These methods follow the official OpenAPI specification. Several of them drive
// devices that are not available to the porting author (floor heaters, light projectors,
// SESAME bot, BLE macros, power distribution boards, Nature EV Switch, ECHONET Lite),
// so they have not been verified against real hardware.
public partial class NatureRemoApi
{
    // ---- helpers ----------------------------------------------------------

    /// <summary>POST a JSON body (a few endpoints do not accept form data).</summary>
    private async Task SendJsonAsync(string endpoint, object body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        request.Headers.UserAgent.ParseAdd($"nature-remo/{Version} ({ProjectUrl})");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        HttpResponseMessage resp;
        try
        {
            resp = await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            throw new NatureRemoException(e.Message, e);
        }

        await EnsureOkAsync(resp).ConfigureAwait(false);
    }

    /// <summary>Build the form shared by the air conditioner / floor heater settings endpoints.</summary>
    private static List<KeyValuePair<string, string>> BuildAirconForm(
        string? operationMode,
        string? temperature,
        string? airVolume,
        string? airDirection,
        string? button,
        string? airDirectionH,
        string? temperatureUnit,
        IReadOnlyDictionary<string, string>? extra)
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

        return data;
    }

    // ---- air conditioner / floor heater -----------------------------------

    /// <summary>
    /// Update air conditioner settings using the v2 endpoint, which returns the whole
    /// updated appliance. Only non-empty values are sent.
    /// </summary>
    /// <param name="appliance">Appliance ID.</param>
    /// <param name="operationMode">AC operation mode.</param>
    /// <param name="temperature">Temperature.</param>
    /// <param name="airVolume">AC air volume.</param>
    /// <param name="airDirection">AC air direction.</param>
    /// <param name="button">Button ("power-off" turns the unit off).</param>
    /// <param name="airDirectionH">AC horizontal air direction.</param>
    /// <param name="temperatureUnit">Temperature unit, "c" or "f".</param>
    /// <param name="extra">AC specific options, sent as extra.{id}={value}.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated appliance.</returns>
    public Task<Appliance> UpdateAirconSettingsV2Async(
        string appliance,
        string? operationMode = null,
        string? temperature = null,
        string? airVolume = null,
        string? airDirection = null,
        string? button = null,
        string? airDirectionH = null,
        string? temperatureUnit = null,
        IReadOnlyDictionary<string, string>? extra = null,
        CancellationToken ct = default) =>
        PostJsonAsync<Appliance>(
            $"/2/appliances/{appliance}/aircon_settings",
            BuildAirconForm(operationMode, temperature, airVolume, airDirection, button,
                airDirectionH, temperatureUnit, extra),
            ct);

    /// <summary>Update floor heater settings. Only non-empty values are sent.</summary>
    /// <param name="appliance">Appliance ID.</param>
    /// <param name="operationMode">Operation mode.</param>
    /// <param name="temperature">Temperature.</param>
    /// <param name="airVolume">Air volume.</param>
    /// <param name="airDirection">Air direction.</param>
    /// <param name="button">Button ("power-off" turns the unit off).</param>
    /// <param name="airDirectionH">Horizontal air direction.</param>
    /// <param name="temperatureUnit">Temperature unit, "c" or "f".</param>
    /// <param name="extra">Device specific options, sent as extra.{id}={value}.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated appliance.</returns>
    public Task<Appliance> UpdateFloorHeaterSettingsAsync(
        string appliance,
        string? operationMode = null,
        string? temperature = null,
        string? airVolume = null,
        string? airDirection = null,
        string? button = null,
        string? airDirectionH = null,
        string? temperatureUnit = null,
        IReadOnlyDictionary<string, string>? extra = null,
        CancellationToken ct = default) =>
        PostJsonAsync<Appliance>(
            $"/1/appliances/{appliance}/floor_heater_settings",
            BuildAirconForm(operationMode, temperature, airVolume, airDirection, button,
                airDirectionH, temperatureUnit, extra),
            ct);

    // ---- infrared / BLE devices -------------------------------------------

    /// <summary>Send a light projector signal.</summary>
    /// <param name="appliance">Appliance ID.</param>
    /// <param name="button">Button name.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task SendLightProjectorSignalAsync(string appliance, string button, CancellationToken ct = default) =>
        PostAsync($"/1/appliances/{appliance}/light_projector", Form(("button", button)), ct);

    /// <summary>Send a SESAME bot click request.</summary>
    /// <param name="appliance">Appliance ID.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task ClickSesameBotAsync(string appliance, CancellationToken ct = default) =>
        PostAsync($"/1/appliances/{appliance}/sesame_bot/click", null, ct);

    /// <summary>Fetch the list of BLE private macros of an appliance.</summary>
    /// <param name="appliance">Appliance ID.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<List<BlePrivateMacro>> GetBlePrivateMacrosAsync(string appliance, CancellationToken ct = default) =>
        FetchListAsync<BlePrivateMacro>($"/1/ble/appliances/{appliance}/private_macros", ct);

    /// <summary>Execute a BLE private macro.</summary>
    /// <param name="privateMacro">Private macro ID.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task ExecuteBlePrivateMacroAsync(string privateMacro, CancellationToken ct = default) =>
        PostAsync($"/1/ble/private_macros/{privateMacro}/exec", null, ct);

    // ---- power distribution board -----------------------------------------

    /// <summary>Fetch the power distribution board dashboard of an appliance.</summary>
    /// <param name="appliance">Appliance ID.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<PowerDistBoardDashboard> GetPowerDistBoardAsync(string appliance, CancellationToken ct = default) =>
        FetchAsync<PowerDistBoardDashboard>($"/1/appliances/{appliance}/power_dist_board", ct);

    /// <summary>Update the names of a power distribution board channel.</summary>
    /// <param name="appliance">Appliance ID.</param>
    /// <param name="number">Channel number (1-252).</param>
    /// <param name="roomName">Room name (max 40 chars).</param>
    /// <param name="applianceName">Appliance name (max 40 chars).</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<PowerDistBoardChannel> UpdatePowerDistBoardChannelAsync(
        string appliance, int number, string roomName, string applianceName, CancellationToken ct = default) =>
        PostJsonAsync<PowerDistBoardChannel>(
            $"/1/appliances/{appliance}/power_dist_board/channel",
            Form(("number", number.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                 ("room_name", roomName), ("appliance_name", applianceName)),
            ct);

    // ---- Nature EV Switch -------------------------------------------------

    /// <summary>Update the relay state of a Nature EV Switch (requires the "nature_evcc" scope).</summary>
    /// <param name="appliance">Appliance ID.</param>
    /// <param name="state">Relay state to request.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task UpdateEvccRelayStateAsync(string appliance, EvccRelayState state, CancellationToken ct = default) =>
        SendJsonAsync(
            $"/1/evcc/appliances/{appliance}/state",
            new { relay_state = state == EvccRelayState.Open ? "open" : "close" },
            ct);

    // ---- ECHONET Lite -----------------------------------------------------

    /// <summary>
    /// Ask Remo E / Remo E lite to refresh ECHONET Lite properties. Subject to an ECHONET Lite
    /// specific rate limit: wait for the device to respond (or about 20 seconds) before calling again.
    /// The server accepts at most 12 EPCs per call.
    /// </summary>
    /// <param name="appliance">ECHONET Lite appliance ID.</param>
    /// <param name="epcs">EPCs as hex strings, for example "cf" and "da".</param>
    /// <param name="ct">Cancellation token.</param>
    public Task RefreshEchonetLitePropertiesAsync(
        string appliance, IEnumerable<string> epcs, CancellationToken ct = default) =>
        PostAsync($"/1/echonetlite/appliances/{appliance}/refresh",
            Form(("epc", string.Join(",", epcs))), ct);

    /// <summary>
    /// Write one ECHONET Lite property. This is a support option that Nature only enables for
    /// approved customers, and it is subject to the ECHONET Lite specific rate limit.
    /// </summary>
    /// <param name="appliance">ECHONET Lite appliance ID.</param>
    /// <param name="epc">EPC as a hex string, for example "cf".</param>
    /// <param name="val">
    /// Value as a hex string. Its length must be 2x the number of bytes defined by the
    /// ECHONET Lite specification, zero-padded if necessary (for example "000000FF").
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    public Task SetEchonetLitePropertyAsync(
        string appliance, string epc, string val, CancellationToken ct = default) =>
        PostAsync($"/1/echonetlite/appliances/{appliance}/set", Form(("epc", epc), ("val", val)), ct);

    // ---- appliance detection (v2) -----------------------------------------

    /// <summary>
    /// Find the air conditioner best matching the provided infrared signal (v2). The response
    /// carries a session token that can be passed to the next call to narrow down the candidates.
    /// </summary>
    /// <param name="message">JSON string with "data", "freq" and "format" keys.</param>
    /// <param name="device">Device ID, to filter the result by device capability (optional).</param>
    /// <param name="sessionToken">Session token returned by a previous call (optional).</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<DetectApplianceV2Response> DetectApplianceV2Async(
        string message,
        string? device = null,
        string? sessionToken = null,
        CancellationToken ct = default) =>
        PostJsonAsync<DetectApplianceV2Response>(
            "/2/detectappliance",
            Form(("message", message), ("device", device), ("session_token", sessionToken)),
            ct);
}
