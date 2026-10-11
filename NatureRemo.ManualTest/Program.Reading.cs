namespace NatureRemo.ManualTest;

// Menu items 11-14: read-only endpoints.
internal static partial class Program
{
    private static async Task ShowDeviceAppliancesAsync(NatureRemoApi api)
    {
        var device = await PickDeviceAsync(api);
        if (device is null) return;

        var appliances = await api.GetDeviceAppliancesAsync(device.Id);
        Console.WriteLine($"{appliances.Count} appliance(s) under '{device.Name}'");
        foreach (var a in appliances)
            Console.WriteLine($"- {a.Nickname}  type={a.Type}  id={a.Id}");
    }

    private static async Task ShowHomesAsync(NatureRemoApi api)
    {
        var homes = await api.GetHomesAsync();
        Console.WriteLine($"{homes.Count} home(s)");
        for (var i = 0; i < homes.Count; i++)
        {
            var h = homes[i];
            var breaker = h.BreakerCapacity is { } a ? $"{a}A" : "-";
            Console.WriteLine($"  [{i + 1}] {h.Name}  breaker={breaker}  members={h.Users?.Count ?? 0}");
            foreach (var u in h.Users ?? [])
                Console.WriteLine($"        {u.Nickname} ({u.Role})");
        }
        if (homes.Count == 0) return;

        Console.Write("Show the devices of a home? Enter its number (blank to skip): ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var n) || n < 1 || n > homes.Count)
            return;

        var devices = await api.GetHomeDevicesAsync(homes[n - 1].Id);
        Console.WriteLine($"{devices.Count} device(s) in '{homes[n - 1].Name}'");
        foreach (var d in devices)
            Console.WriteLine($"- {d.Name}  firmware={d.FirmwareVersion}  online={d.Online?.ToString() ?? "-"}");

        var users = await api.GetHomeUsersAsync(homes[n - 1].Id);
        Console.WriteLine($"{users.Count} user(s)");
        foreach (var u in users)
            Console.WriteLine($"- {u.User?.Nickname} ({u.Role})");
    }

    private static async Task ShowElectricityAsync(NatureRemoApi api)
    {
        var device = await PickDeviceAsync(api);
        if (device is null) return;

        var series = await api.GetHourlyElectricityAsync(device.Id);
        var metrics = new (string Name, List<List<double>>? Data)[]
        {
            ("home_consumed", series.HomeConsumed),
            ("from_grid", series.FromGrid),
            ("to_grid", series.ToGrid),
            ("from_solar_power", series.FromSolarPower),
            ("from_storage_battery", series.FromStorageBattery),
            ("to_storage_battery", series.ToStorageBattery),
            ("from_fuel_cell", series.FromFuelCell),
            ("from_evcd", series.FromEvcd),
            ("to_evcd", series.ToEvcd),
            ("to_evcc", series.ToEvcc),
        };

        var any = false;
        foreach (var (name, data) in metrics)
        {
            var points = DeviceEnergyTimeSeries.ToPoints(data);
            if (points.Count == 0) continue;

            any = true;
            Console.WriteLine($"{name}: {points.Count} point(s)");
            foreach (var p in points.TakeLast(3))
                Console.WriteLine($"    {p.Time.ToLocalTime():yyyy-MM-dd HH:mm}  {p.Kwh:0.###} kWh");
        }

        if (!any)
            Console.WriteLine("No energy data. (A Remo mini has no electricity data.)");
    }

    private static async Task ShowEchonetLiteAsync(NatureRemoApi api)
    {
        var appliances = await api.GetEchonetLiteAppliancesAsync();
        Console.WriteLine($"{appliances.Count} ECHONET Lite appliance(s)");
        foreach (var a in appliances)
        {
            var props = a.Properties ?? [];
            Console.WriteLine($"- {a.Nickname}  type={a.Type}  properties={props.Count}");
            foreach (var p in props.Take(5))
                Console.WriteLine($"    epc={p.Epc} (0x{p.ParseEpc():x2})  val={p.Val}  updated={p.UpdatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
            if (props.Count > 5)
                Console.WriteLine($"    ... {props.Count - 5} more");
        }
    }

    private static async Task<Device?> PickDeviceAsync(NatureRemoApi api)
    {
        var devices = await api.GetDevicesAsync();
        if (devices.Count == 0)
        {
            Console.WriteLine("No devices found.");
            return null;
        }
        if (devices.Count == 1)
        {
            Console.WriteLine($"Using device '{devices[0].Name}'.");
            return devices[0];
        }

        for (var i = 0; i < devices.Count; i++)
            Console.WriteLine($"  [{i + 1}] {devices[i].Name}");

        Console.Write("Device number: ");
        if (int.TryParse(Console.ReadLine()?.Trim(), out var n) && n >= 1 && n <= devices.Count)
            return devices[n - 1];

        Console.WriteLine("Cancelled.");
        return null;
    }
}
