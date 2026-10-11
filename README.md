# NatureRemo

A .NET client library for the [Nature Remo](https://nature.global/) **Cloud API** and **Local API**.

- Async-first API with `CancellationToken` support on every call
- Strongly typed models for appliances, devices, homes, signals, and more
- Rate-limit information from the latest response (`api.RateLimit`)
- Targets .NET 8 or later

> This is an unofficial library and is not affiliated with Nature, Inc.
> It is a C# port of, and extension to, [morinokami/nature-remo](https://github.com/morinokami/nature-remo) (Python).

## Installation

```sh
dotnet add package NatureRemo
```

> Replace `NatureRemo` with the actual package ID if it differs.

## Authentication

The Cloud API requires an access token. Generate a personal access token at
[home.nature.global](https://home.nature.global/) and pass it to `NatureRemoApi`.

This library sends the token as a Bearer token. It does **not** implement the OAuth2 authorization
flow (client registration for OAuth2 is limited to corporate users). If you obtain a token through OAuth2
yourself, you can pass it in the same way.

Do not hard-code tokens in source code. Store them in a secure location such as environment
variables, a secret store, or the OS credential vault.

## Quick start

```csharp
using NatureRemo;

using var api = new NatureRemoApi(Environment.GetEnvironmentVariable("NATURE_REMO_TOKEN")!);

// Fetch every appliance you have access to.
var appliances = await api.GetAppliancesAsync();

foreach (var appliance in appliances)
    Console.WriteLine($"{appliance.Nickname} ({appliance.Type})");
```

### Controlling an air conditioner

Only the values you pass are sent. Use `button: "power-off"` to turn the unit off.

```csharp
var ac = appliances.First(a => a.AirCon is not null);

// The valid modes, temperatures, volumes, and directions depend on the model.
var modes = ac.AirCon!.Range?.Modes;

var settings = await api.UpdateAirconSettingsAsync(
    ac.Id, operationMode: "cool", temperature: "26");

await api.UpdateAirconSettingsAsync(ac.Id, button: "power-off");
```

> Infrared control is one-way. `Appliance.Settings` reflects the last settings **sent**,
> not the actual state of the air conditioner.

### TV, lights, and learned signals

```csharp
// TV / light: use the button names listed on the appliance.
var tv = appliances.First(a => a.Tv is not null);
await api.SendTvInfraredSignalAsync(tv.Id, tv.Tv!.Buttons?.First().Name ?? "power");

var light = appliances.First(a => a.Light is not null);
await api.SendLightInfraredSignalAsync(light.Id, "on");

// Learned (custom) signals.
foreach (var signal in appliances.SelectMany(a => a.Signals ?? []))
    Console.WriteLine($"{signal.Name}: {signal.Id}");

await api.SendSignalAsync("signal-id");
```

### Homes and devices

```csharp
var homes = await api.GetHomesAsync();

foreach (var home in homes)
{
    var devices = await api.GetHomeDevicesAsync(home.Id);
    Console.WriteLine($"{home.Name}: {devices.Count} device(s)");
}
```

`Appliance.Device.Id` identifies the Remo device that controls an appliance, so you can group
appliances by home using the result above. Note that `Appliance.Device` can be `null`.

### Electricity data

Requires the `electricity` scope.

```csharp
var series = await api.GetHourlyElectricityAsync("device-id");

foreach (var point in DeviceEnergyTimeSeries.ToPoints(series.HomeConsumed))
    Console.WriteLine($"{point.Time:g}: {point.Kwh} kWh");
```

### Local API

`NatureRemoLocalApi` talks to a Remo on your local network. It can only read the newest received
infrared signal and emit infrared signals; it cannot list appliances.

```csharp
using var local = new NatureRemoLocalApi("192.168.1.20");

var signal = await local.GetIRSignalAsync();
await local.SendIRSignalAsync(signal.AsJsonString());
```

## Error handling and rate limits

Failures are reported as `NatureRemoException`. For HTTP errors the message includes the status
code and the Nature Remo error code and message.

```csharp
try
{
    await api.GetAppliancesAsync();
}
catch (NatureRemoException ex)
{
    Console.Error.WriteLine(ex.Message);
}

Console.WriteLine($"Remaining requests: {api.RateLimit.Remaining} (resets {api.RateLimit.Reset})");
```

The Cloud API is rate limited. Cache the appliance list, update local state from the response of each
operation instead of re-fetching, and avoid sending requests in a tight loop.

## Using your own `HttpClient`

Pass an `HttpClient` to reuse connections, for example with `IHttpClientFactory`.
A client you pass in is not disposed by this library.

```csharp
services.AddHttpClient("nature");
services.AddSingleton(sp => new NatureRemoApi(
    token, sp.GetRequiredService<IHttpClientFactory>().CreateClient("nature")));
```

## API coverage

**Cloud API**

| Area | Methods |
|---|---|
| User | `GetUserAsync`, `UpdateUserAsync` |
| Devices | `GetDevicesAsync`, `UpdateDeviceAsync`, `DeleteDeviceAsync`, `UpdateTemperatureOffsetAsync`, `UpdateHumidityOffsetAsync`, `GetDeviceAppliancesAsync`, `GetHourlyElectricityAsync` |
| Appliances | `GetAppliancesAsync`, `CreateApplianceAsync`, `UpdateApplianceAsync`, `DeleteApplianceAsync`, `UpdateApplianceOrdersAsync` |
| Appliance detection | `DetectApplianceAsync`, `DetectApplianceV2Async` |
| Air conditioner / floor heater | `UpdateAirconSettingsAsync`, `UpdateAirconSettingsV2Async`, `UpdateFloorHeaterSettingsAsync` |
| TV / light / light projector | `SendTvInfraredSignalAsync`, `SendLightInfraredSignalAsync`, `SendLightProjectorSignalAsync` |
| Signals | `GetSignalsAsync`, `CreateSignalAsync`, `UpdateSignalAsync`, `DeleteSignalAsync`, `UpdateSignalOrdersAsync`, `SendSignalAsync` |
| BLE | `GetBlePrivateMacrosAsync`, `ExecuteBlePrivateMacroAsync`, `ClickSesameBotAsync` |
| Power distribution board | `GetPowerDistBoardAsync`, `UpdatePowerDistBoardChannelAsync` |
| Nature EV Switch | `UpdateEvccRelayStateAsync` |
| ECHONET Lite | `GetEchonetLiteAppliancesAsync`, `RefreshEchonetLitePropertiesAsync`, `SetEchonetLitePropertyAsync` |
| Homes | `GetHomesAsync`, `GetHomeDevicesAsync`, `GetHomeUsersAsync`, `CreateHomeAsync`, `UpdateHomeAsync`, `DeleteHomeAsync`, `PartHomeAsync`, `ChangeHomeOwnerAsync`, `KickHomeUserAsync`, `TransferHomeDevicesAsync`, `UpdateHomeLocationAsync`, `DeleteHomeLocationAsync`, `UpdateHomeLocationStateAsync` |
| Invites | `CreateHomeInviteAsync`, `GetHomeInviteAsync`, `AcceptHomeInviteAsync` |

**Local API:** `GetIRSignalAsync`, `SendIRSignalAsync`

### Verification status

The methods follow the official OpenAPI specification. Many of them drive hardware or account
features that the author does not have access to, so they have **not been verified** against real
devices or accounts:

- Floor heaters, light projectors, SESAME bot, BLE private macros
- Power distribution boards, Nature EV Switch, ECHONET Lite refresh/set
- Home management and invites (create, delete, kick, owner change, transfer, location)

`UpdateHomeLocationAsync` and `UpdateHomeLocationStateAsync` take the raw form fields as a dictionary
because the specification does not describe their request parameters.

Home management operations such as deleting a home, kicking a user, and transferring devices are
destructive. Use them with care.

Bug reports and pull requests are welcome.

## Notes on models

- Model classes mirror the JSON returned by the API. Properties that the specification marks as
  nullable are nullable in C#; check for `null` before using them (for example `Appliance.Signals`,
  `TV.Buttons`, `Device.NewestEvents`).
- Every model has `AsJsonString()` for serialization and a readable `ToString()`.

## Acknowledgements

This library is based on [morinokami/nature-remo](https://github.com/morinokami/nature-remo) and the
[Nature Remo Cloud API](https://swagger.nature.global/) specification.

## License

See the `LICENSE` file.
