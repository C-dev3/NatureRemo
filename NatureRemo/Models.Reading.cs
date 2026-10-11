// Models for the read-only endpoints in NatureRemoApi.Reading.cs.
// They mirror the JSON returned by the Nature Remo API one-to-one, so missing-XML-doc
// warnings are suppressed here, as in Models.cs.
#pragma warning disable CS1591

using System.Globalization;
using System.Text.Json.Serialization;

namespace NatureRemo;

// ---- homes ----------------------------------------------------------------

public class HomeMember : NatureRemoModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("nickname")] public string Nickname { get; set; } = "";
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("location_state")] public string LocationState { get; set; } = "";
    [JsonPropertyName("joined_at")] public DateTimeOffset JoinedAt { get; set; }

    public override string ToString() =>
        $"HomeMember(id='{Id}', nickname='{Nickname}', role='{Role}', " +
        $"location_state='{LocationState}', joined_at={JoinedAt:o})";
}

public class Home : NatureRemoModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    /// <summary>Breaker capacity in amperes (10 to 200), if set.</summary>
    [JsonPropertyName("breaker_capacity")] public int? BreakerCapacity { get; set; }

    [JsonPropertyName("users")] public List<HomeMember>? Users { get; set; }

    public override string ToString() =>
        $"Home(id='{Id}', name='{Name}', breaker_capacity={BreakerCapacity?.ToString() ?? "null"}, " +
        $"users=[{string.Join(", ", Users ?? [])}])";
}

public class HomeUserRole : NatureRemoModel
{
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("user")] public User? User { get; set; }

    public override string ToString() => $"HomeUserRole(role='{Role}', user={User})";
}

// ---- electricity ----------------------------------------------------------

/// <summary>One data point of an energy series: the start of the hour and the energy in kWh.</summary>
public readonly record struct EnergyPoint(DateTimeOffset Time, double Kwh);

public class DeviceEnergyTimeSeries : NatureRemoModel
{
    // Each series is a list of [unix time in seconds, value in kWh] pairs.
    [JsonPropertyName("home_consumed")] public List<List<double>>? HomeConsumed { get; set; }
    [JsonPropertyName("from_grid")] public List<List<double>>? FromGrid { get; set; }
    [JsonPropertyName("to_grid")] public List<List<double>>? ToGrid { get; set; }
    [JsonPropertyName("from_solar_power")] public List<List<double>>? FromSolarPower { get; set; }
    [JsonPropertyName("from_storage_battery")] public List<List<double>>? FromStorageBattery { get; set; }
    [JsonPropertyName("to_storage_battery")] public List<List<double>>? ToStorageBattery { get; set; }
    [JsonPropertyName("from_fuel_cell")] public List<List<double>>? FromFuelCell { get; set; }
    [JsonPropertyName("from_evcd")] public List<List<double>>? FromEvcd { get; set; }
    [JsonPropertyName("to_evcd")] public List<List<double>>? ToEvcd { get; set; }
    [JsonPropertyName("to_evcc")] public List<List<double>>? ToEvcc { get; set; }

    /// <summary>Convert a raw [unix time, kWh] series into typed points.</summary>
    public static IReadOnlyList<EnergyPoint> ToPoints(List<List<double>>? series) =>
        [.. (series ?? [])
            .Where(pair => pair.Count == 2)
            .Select(pair => new EnergyPoint(DateTimeOffset.FromUnixTimeSeconds((long)pair[0]), pair[1]))];
}

// ---- ECHONET Lite ---------------------------------------------------------

/// <summary>
/// A property row of an ECHONET Lite appliance. Unlike the smart meter properties on
/// <see cref="Appliance"/>, <c>epc</c> and <c>val</c> are hexadecimal strings here.
/// </summary>
public class EchonetLitePropertyRow : NatureRemoModel
{
    [JsonPropertyName("epc")] public string Epc { get; set; } = "";
    [JsonPropertyName("val")] public string Val { get; set; } = "";
    [JsonPropertyName("updated_at")] public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Parse <see cref="Epc"/> (a hex string such as "e7") into a number, or null if invalid.</summary>
    public int? ParseEpc() =>
        int.TryParse(Epc, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) ? value : null;

    public override string ToString() => $"EchonetLitePropertyRow(epc='{Epc}', val='{Val}', updated_at={UpdatedAt:o})";
}

public class EchonetLiteAppliance : NatureRemoModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("nickname")] public string Nickname { get; set; } = "";

    /// <summary>For example EL_STORAGE_BATTERY, EL_SOLAR_POWER, EL_EVCD or EL_ELECTRIC_WATER_HEATER.</summary>
    [JsonPropertyName("type")] public string Type { get; set; } = "";

    // The specification spells this key "Device"; deserialization is case-insensitive.
    [JsonPropertyName("device")] public DeviceCore? Device { get; set; }

    [JsonPropertyName("properties")] public List<EchonetLitePropertyRow>? Properties { get; set; }

    public override string ToString() =>
        $"EchonetLiteAppliance(id='{Id}', nickname='{Nickname}', type='{Type}', device={Device}, " +
        $"properties=[{string.Join(", ", Properties ?? [])}])";
}

internal class EchonetLiteApplianceResponse
{
    [JsonPropertyName("appliances")] public List<EchonetLiteAppliance>? Appliances { get; set; }
}
