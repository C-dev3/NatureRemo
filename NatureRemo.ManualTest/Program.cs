using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NatureRemo.ManualTest;

/// <summary>
/// Interactive console for testing the NatureRemo library against a real Nature Remo account.
/// The access token is typed in by the user at startup and is never written to disk or printed.
/// Menu items marked [WRITE] change real devices and always ask for confirmation first.
/// </summary>
internal static partial class Program
{
    private static async Task<int> Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("NatureRemo manual test console");
        Console.WriteLine("==============================");

        var token = ReadAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.Error.WriteLine("An access token is required.");
            return 1;
        }

        using var api = new NatureRemoApi(token);

        while (true)
        {
            PrintMenu();
            var choice = Console.ReadLine()?.Trim();
            if (choice is null or "q" or "Q")
                return 0;

            // Menu 6 already prints the rate limit itself.
            await RunAsync(api, () => HandleAsync(api, choice), printRateLimit: choice != "6");
        }
    }

    // ---- menu -------------------------------------------------------------

    private static void PrintMenu()
    {
        Console.WriteLine();
        Console.WriteLine("  1  Get user");
        Console.WriteLine("  2  Get devices (with newest sensor values)");
        Console.WriteLine("  3  Get appliances");
        Console.WriteLine("  4  Get signals of an appliance");
        Console.WriteLine("  5  Run all read-only checks");
        Console.WriteLine("  6  Show rate limit");
        Console.WriteLine("  7  Local API: get newest received IR signal");
        Console.WriteLine("  8  [WRITE] Send a registered signal");
        Console.WriteLine("  9  [WRITE] Update air conditioner settings");
        Console.WriteLine("  10 [WRITE] Send a light button (on / off / ...)");
        Console.WriteLine("  11 Get appliances of a device");
        Console.WriteLine("  12 Get homes (and their devices / users)");
        Console.WriteLine("  13 Get hourly electricity");
        Console.WriteLine("  14 Get ECHONET Lite appliances");
        Console.WriteLine("  15 Detect appliance (v2) from the latest local IR signal");
        Console.WriteLine("  16 [WRITE] Update air conditioner temperature (v2 endpoint)");
        Console.WriteLine("  q  Quit");
        Console.Write("> ");
    }

    private static Task HandleAsync(NatureRemoApi api, string choice) => choice switch
    {
        "1" => ShowUserAsync(api),
        "2" => ShowDevicesAsync(api),
        "3" => ShowAppliancesAsync(api),
        "4" => ShowSignalsAsync(api),
        "5" => RunAllReadOnlyChecksAsync(api),
        "6" => Task.Run(() => PrintRateLimit(api, always: true)),
        "7" => ShowLocalIrSignalAsync(),
        "8" => SendSignalAsync(api),
        "9" => UpdateAirconAsync(api),
        "10" => SendLightButtonAsync(api),
        "11" => ShowDeviceAppliancesAsync(api),
        "12" => ShowHomesAsync(api),
        "13" => ShowElectricityAsync(api),
        "14" => ShowEchonetLiteAsync(api),
        "15" => ShowDetectV2Async(api),
        "16" => UpdateAirconV2Async(api),
        _ => Task.Run(() => Console.WriteLine("Unknown menu item.")),
    };

    private static async Task RunAsync(NatureRemoApi api, Func<Task> action, bool printRateLimit = true)
    {
        try
        {
            await action();
        }
        catch (NatureRemoException e)
        {
            Console.WriteLine($"[NatureRemoException] {e.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[{e.GetType().Name}] {e.Message}");
        }
        finally
        {
            if (printRateLimit)
                PrintRateLimit(api, always: false);
        }
    }

    // ---- read-only actions ------------------------------------------------

    private static async Task ShowUserAsync(NatureRemoApi api)
    {
        var user = await api.GetUserAsync();
        Console.WriteLine(Pretty(user.AsJsonString()));
    }

    private static async Task ShowDevicesAsync(NatureRemoApi api)
    {
        var devices = await api.GetDevicesAsync();
        Console.WriteLine($"{devices.Count} device(s)");
        foreach (var d in devices)
        {
            Console.WriteLine($"- {d.Name}  id={d.Id}  firmware={d.FirmwareVersion}");
            foreach (var (kind, value) in d.NewestEvents ?? [])
                Console.WriteLine($"    {kind}: {value.Val}  ({value.CreatedAt:yyyy-MM-dd HH:mm:ss zzz})");
        }
    }

    private static async Task ShowAppliancesAsync(NatureRemoApi api)
    {
        var appliances = await api.GetAppliancesAsync();
        Console.WriteLine($"{appliances.Count} appliance(s)");
        foreach (var a in appliances)
            Console.WriteLine($"- {a.Nickname}  type={a.Type}  id={a.Id}  signals={a?.Signals?.Count ?? 0}");

        Console.Write("Print full JSON of one appliance? Enter its number (blank to skip): ");
        var input = Console.ReadLine()?.Trim();
        if (int.TryParse(input, out var n) && n >= 1 && n <= appliances.Count)
            Console.WriteLine(Pretty(appliances[n - 1].AsJsonString()));
        else if (appliances.Count > 0)
            Console.WriteLine("(Numbers follow the order listed above, starting from 1.)");
    }

    private static async Task ShowSignalsAsync(NatureRemoApi api)
    {
        var appliance = await PickApplianceAsync(api);
        if (appliance is null) return;

        var signals = await api.GetSignalsAsync(appliance.Id);
        Console.WriteLine($"{signals.Count} signal(s) under '{appliance.Nickname}'");
        foreach (var s in signals)
            Console.WriteLine($"- {s.Name}  id={s.Id}  image={s.Image}");
    }

    private static async Task RunAllReadOnlyChecksAsync(NatureRemoApi api)
    {
        var results = new List<(string Name, bool Ok, string Detail)>();

        async Task CheckAsync(string name, Func<Task<string>> action)
        {
            try
            {
                var detail = await action();
                results.Add((name, true, detail));
            }
            catch (Exception e)
            {
                results.Add((name, false, e.Message));
            }
        }

        List<Appliance> appliances = [];

        await CheckAsync("GetUserAsync", async () =>
        {
            var u = await api.GetUserAsync();
            return $"nickname={u.Nickname}";
        });
        await CheckAsync("GetDevicesAsync", async () =>
        {
            var d = await api.GetDevicesAsync();
            return $"{d.Count} device(s)";
        });
        await CheckAsync("GetAppliancesAsync", async () =>
        {
            appliances = await api.GetAppliancesAsync();
            return $"{appliances.Count} appliance(s)";
        });

        foreach (var a in appliances)
        {
            var target = a;
            await CheckAsync($"GetSignalsAsync ({target.Nickname})", async () =>
            {
                var s = await api.GetSignalsAsync(target.Id);
                return $"{s.Count} signal(s)";
            });
        }

        Console.WriteLine();
        Console.WriteLine("Result");
        Console.WriteLine("------");
        foreach (var (name, ok, detail) in results)
            Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}  -  {detail}");

        var failed = results.Count(r => !r.Ok);
        Console.WriteLine(failed == 0
            ? $"All {results.Count} checks passed."
            : $"{failed} of {results.Count} checks failed.");
    }

    private static async Task ShowLocalIrSignalAsync()
    {
        Console.WriteLine("Point a remote at your Remo and press a button first, then enter Remo's local IP address.");
        Console.Write("Remo IP address: ");
        var addr = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(addr)) return;

        using var local = new NatureRemoLocalApi(addr);
        var signal = await local.GetIRSignalAsync();
        Console.WriteLine(Pretty(signal.AsJsonString()));
    }

    // ---- write actions (confirmation required) ----------------------------

    private static async Task SendSignalAsync(NatureRemoApi api)
    {
        var appliance = await PickApplianceAsync(api);
        if (appliance is null) return;

        var signals = await api.GetSignalsAsync(appliance.Id);
        if (signals.Count == 0)
        {
            Console.WriteLine("This appliance has no registered signals.");
            return;
        }

        for (var i = 0; i < signals.Count; i++)
            Console.WriteLine($"  [{i + 1}] {signals[i].Name}");

        Console.Write("Signal number: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var n) || n < 1 || n > signals.Count)
        {
            Console.WriteLine("Cancelled.");
            return;
        }

        var signal = signals[n - 1];
        if (!Confirm($"Send '{signal.Name}' via '{appliance.Nickname}'?")) return;

        await api.SendSignalAsync(signal.Id);
        Console.WriteLine("Sent.");
    }

    private static async Task UpdateAirconAsync(NatureRemoApi api)
    {
        var appliance = await PickApplianceAsync(api);
        if (appliance is null) return;

        Console.WriteLine("Leave a value blank to leave it unchanged.");
        var mode = Prompt("operation_mode (e.g. cool / warm / dry / blow / auto)");
        var temp = Prompt("temperature (e.g. 26)");
        var vol = Prompt("air_volume (e.g. auto / 1)");
        var dir = Prompt("air_direction (e.g. auto / swing)");
        var button = Prompt("button (\"power-off\" to turn off, blank otherwise)");

        if (mode is null && temp is null && vol is null && dir is null && button is null)
        {
            Console.WriteLine("Nothing to change.");
            return;
        }

        Console.WriteLine($"Target: {appliance.Nickname}  mode={mode ?? "-"}  temp={temp ?? "-"}  " +
                          $"vol={vol ?? "-"}  dir={dir ?? "-"}  button={button ?? "-"}");
        if (!Confirm("Apply these settings?")) return;

        var result = await api.UpdateAirconSettingsAsync(
            appliance.Id,
            operationMode: mode,
            temperature: temp,
            airVolume: vol,
            airDirection: dir,
            button: button);
        Console.WriteLine($"Updated. Now: mode={result.Mode} temp={result.Temp} vol={result.Vol} " +
                          $"dir={result.Dir} button={result.Button} updated_at={result.UpdatedAt:u}");
    }

    private static async Task SendLightButtonAsync(NatureRemoApi api)
    {
        var appliance = await PickApplianceAsync(api, type: "LIGHT");
        if (appliance is null) return;

        var buttons = appliance.Light?.Buttons ?? [];
        if (buttons.Count == 0)
        {
            Console.WriteLine("This appliance has no light buttons.");
            return;
        }

        if (appliance.Light?.State is { } before)
            Console.WriteLine($"Current state: power={before.Power} brightness={before.Brightness} last_button={before.LastButton}");

        for (var i = 0; i < buttons.Count; i++)
            Console.WriteLine($"  [{i + 1}] {buttons[i].Name}  ({buttons[i].Label})");

        Console.Write("Button number: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var n) || n < 1 || n > buttons.Count)
        {
            Console.WriteLine("Cancelled.");
            return;
        }

        var button = buttons[n - 1];
        if (!Confirm($"Send '{button.Name}' to '{appliance.Nickname}'?")) return;

        await api.SendLightInfraredSignalAsync(appliance.Id, button.Name);
        Console.WriteLine("Sent.");

        // Re-fetch to show the state as Nature Remo now sees it.
        await Task.Delay(1000);
        var updated = (await api.GetAppliancesAsync()).FirstOrDefault(a => a.Id == appliance.Id);
        if (updated?.Light?.State is { } after)
            Console.WriteLine($"New state:     power={after.Power} brightness={after.Brightness} last_button={after.LastButton}");
    }

    // ---- helpers ----------------------------------------------------------

    private static async Task<Appliance?> PickApplianceAsync(NatureRemoApi api, string? type = null)
    {
        var appliances = (await api.GetAppliancesAsync())
            .Where(a => type is null || a.Type == type)
            .ToList();
        if (appliances.Count == 0)
        {
            Console.WriteLine(type is null ? "No appliances found." : $"No {type} appliances found.");
            return null;
        }

        for (var i = 0; i < appliances.Count; i++)
            Console.WriteLine($"  [{i + 1}] {appliances[i].Nickname} ({appliances[i].Type})");

        Console.Write("Appliance number: ");
        if (int.TryParse(Console.ReadLine()?.Trim(), out var n) && n >= 1 && n <= appliances.Count)
            return appliances[n - 1];

        Console.WriteLine("Cancelled.");
        return null;
    }

    private static string? Prompt(string label)
    {
        Console.Write($"{label}: ");
        var value = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static bool Confirm(string message)
    {
        Console.Write($"{message} Type 'yes' to continue: ");
        var ok = string.Equals(Console.ReadLine()?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);
        if (!ok) Console.WriteLine("Cancelled.");
        return ok;
    }

    private static void PrintRateLimit(NatureRemoApi api, bool always)
    {
        var rl = api.RateLimit;
        if (rl.Limit is null && rl.Remaining is null)
        {
            if (always) Console.WriteLine("No rate limit information yet. Call an API method first.");
            return;
        }

        Console.WriteLine($"[rate limit] remaining {rl.Remaining}/{rl.Limit}, resets at {rl.Reset:u}");
    }

    // Cached and reused: creating a new JsonSerializerOptions per call is expensive (CA1869).
    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static string Pretty(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement, PrettyOptions);
    }

    private static string ReadAccessToken()
    {
        var fromEnv = Environment.GetEnvironmentVariable("REMO_ACCESS_TOKEN");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            Console.Write("Use the access token from REMO_ACCESS_TOKEN? [Y/n]: ");
            var answer = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(answer) || answer.Equals("y", StringComparison.OrdinalIgnoreCase))
                return fromEnv.Trim();
        }

        Console.Write("Enter your Nature Remo access token (input is hidden): ");
        return ReadSecret();
    }

    private static string ReadSecret()
    {
        if (Console.IsInputRedirected)
            return Console.ReadLine()?.Trim() ?? "";

        var sb = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0) sb.Length--;
                continue;
            }
            if (!char.IsControl(key.KeyChar))
                sb.Append(key.KeyChar);
        }
        return sb.ToString().Trim();
    }
}