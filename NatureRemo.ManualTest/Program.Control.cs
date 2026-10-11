namespace NatureRemo.ManualTest;

// Menu items 15-16: control endpoints that can be tried with a Remo mini + air conditioner.
// (Floor heater, light projector, SESAME bot, BLE macros, power distribution board,
// Nature EV Switch and ECHONET Lite control are not exercised here: they need other hardware.)
internal static partial class Program
{
    private static async Task ShowDetectV2Async(NatureRemoApi api)
    {
        Console.WriteLine("Point the air conditioner's remote at Remo and press a button, then enter Remo's local IP address.");
        Console.Write("Remo IP address: ");
        var addr = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(addr)) return;

        using var local = new NatureRemoLocalApi(addr);
        var signal = await local.GetIRSignalAsync();
        Console.WriteLine($"Received IR signal: freq={signal.Freq}, {signal.Data.Count} samples, format={signal.Format}");

        var device = await PickDeviceAsync(api);
        var result = await api.DetectApplianceV2Async(signal.AsJsonString(), device?.Id);

        Console.WriteLine($"suggested_action: {result.SuggestedAction}");
        Console.WriteLine($"message: {result.SuggestedActionMessage}");

        var candidates = result.Results ?? [];
        Console.WriteLine($"{candidates.Count} candidate(s)");
        foreach (var c in candidates.Take(5))
        {
            Console.WriteLine($"- {c.Model?.Manufacturer} {c.Model?.Name} (remote {c.Model?.RemoteName})  " +
                              $"mode={c.Params?.Mode} temp={c.Params?.Temp} button={c.Params?.Button}");
        }
    }

    private static async Task UpdateAirconV2Async(NatureRemoApi api)
    {
        var appliance = await PickApplianceAsync(api, type: "AC");
        if (appliance is null) return;

        var temp = Prompt("temperature (blank to cancel)");
        if (temp is null)
        {
            Console.WriteLine("Cancelled.");
            return;
        }

        if (!Confirm($"Set '{appliance.Nickname}' to {temp}?")) return;

        var updated = await api.UpdateAirconSettingsV2Async(appliance.Id, temperature: temp);
        var s = updated.Settings;
        Console.WriteLine($"Updated. Now: mode={s?.Mode} temp={s?.Temp} vol={s?.Vol} dir={s?.Dir} button={s?.Button}");
    }
}
