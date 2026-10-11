// The model classes mirror the JSON returned by the Nature Remo API one-to-one;
// their members are self-explanatory, so missing-XML-doc warnings are suppressed here.
#pragma warning disable CS1591

using System.Text.Json;
using System.Text.Json.Serialization;

namespace NatureRemo;

/// <summary>JSON serialization helpers shared by all models.</summary>
internal static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static readonly JsonSerializerOptions OutputOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default,
    };
}

/// <summary>Base class for Nature Remo models.</summary>
public abstract class NatureRemoModel
{
    public string AsJsonString() =>
        JsonSerializer.Serialize(this, GetType(), Json.OutputOptions);
}

public class User : NatureRemoModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("nickname")] public string Nickname { get; set; } = "";
    [JsonPropertyName("country")] public string? Country { get; set; }
    [JsonPropertyName("distance_unit")] public string? DistanceUnit { get; set; }
    [JsonPropertyName("temp_unit")] public string? TempUnit { get; set; }

    /// <summary>Set when the privacy policy has been revised and needs the user's consent.</summary>
    [JsonPropertyName("updated_privacy_policy")] public PrivacyPolicyRevisionOutline? UpdatedPrivacyPolicy { get; set; }

    public override string ToString() => $"User(id='{Id}', nickname='{Nickname}', country='{Country}', distance_unit='{DistanceUnit}', temp_unit='{TempUnit}', updated_privacy_policy={UpdatedPrivacyPolicy})";
}

public class SensorValue : NatureRemoModel
{
    [JsonPropertyName("val")] public double Val { get; set; }
    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }

    public override string ToString() => $"SensorValue(val={Val}, created_at={CreatedAt:o})";
}

public class DeviceCore : NatureRemoModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("temperature_offset")] public double TemperatureOffset { get; set; }
    [JsonPropertyName("humidity_offset")] public double HumidityOffset { get; set; }
    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }
    [JsonPropertyName("updated_at")] public DateTimeOffset UpdatedAt { get; set; }
    [JsonPropertyName("firmware_version")] public string FirmwareVersion { get; set; } = "";
    [JsonPropertyName("mac_address")] public string MacAddress { get; set; } = "";
    [JsonPropertyName("serial_number")] public string SerialNumber { get; set; } = "";
    [JsonPropertyName("bt_mac_address")] public string? BtMacAddress { get; set; }

    public override string ToString() =>
        $"Device(id='{Id}', name='{Name}', temperature_offset={TemperatureOffset}, " +
        $"humidity_offset={HumidityOffset}, created_at={CreatedAt:o}, updated_at={UpdatedAt:o}, " +
        $"firmware_version='{FirmwareVersion}', mac_address='{MacAddress}', serial_number='{SerialNumber}', bt_mac_address='{BtMacAddress}')";
}

public class Device : DeviceCore
{
    [JsonPropertyName("newest_events")]
    public Dictionary<string, SensorValue>? NewestEvents { get; set; }
    [JsonPropertyName("online")] public bool? Online { get; set; }
    [JsonPropertyName("bridged_devices_limit")] public int? BridgedDevicesLimit { get; set; }
    [JsonPropertyName("commissionee_fabrics_limit")] public int? CommissioneeFabricsLimit { get; set; }
    [JsonPropertyName("commissionee_fabrics")] public List<CommissioneeFabric>? CommissioneeFabrics { get; set; }

    /// <summary>Deprecated by the API. Do not use in new code.</summary>
    [JsonPropertyName("users")] public List<UserDevice>? Users { get; set; }

    public override string ToString() =>
        base.ToString().TrimEnd(')') +
        $", newest_events={{{string.Join(", ", NewestEvents?.Select(kv => $"'{kv.Key}': {kv.Value}") ?? [])}}}, " +
        $"online={Online?.ToString() ?? "null"}, bridged_devices_limit={BridgedDevicesLimit?.ToString() ?? "null"}, " +
        $"commissionee_fabrics_limit={CommissioneeFabricsLimit?.ToString() ?? "null"})";
}

public class ApplianceModel : NatureRemoModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("remote_name")] public string RemoteName { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("image")] public string Image { get; set; } = "";
    [JsonPropertyName("country")] public string? Country { get; set; }
    [JsonPropertyName("series")] public string? Series { get; set; }

    /// <summary>Only present for BLE appliance models.</summary>
    [JsonPropertyName("slug")] public string? Slug { get; set; }

    public override string ToString() =>
        $"ApplianceModel(id='{Id}', manufacturer='{Manufacturer}', remote_name='{RemoteName}', " +
        $"name='{Name}', image='{Image}', country='{Country}', series='{Series}', slug='{Slug}')";
}

public class AirConParams : NatureRemoModel
{
    [JsonPropertyName("temp")] public string Temp { get; set; } = "";
    [JsonPropertyName("mode")] public string Mode { get; set; } = "";
    [JsonPropertyName("vol")] public string Vol { get; set; } = "";
    [JsonPropertyName("dir")] public string Dir { get; set; } = "";
    [JsonPropertyName("button")] public string Button { get; set; } = "";
    [JsonPropertyName("temp_unit")] public string? TempUnit { get; set; }
    [JsonPropertyName("dirh")] public string? Dirh { get; set; }
    [JsonPropertyName("updated_at")] public DateTimeOffset? UpdatedAt { get; set; }
    [JsonPropertyName("extra")] public Dictionary<string, string>? Extra { get; set; }

    public override string ToString() =>
        $"AirConParams(temp='{Temp}', mode='{Mode}', vol='{Vol}', dir='{Dir}', button='{Button}', temp_unit='{TempUnit}', dirh='{Dirh}', updated_at='{UpdatedAt}', extra={{{string.Join(", ", Extra?.Select(kv => $"'{kv.Key}': '{kv.Value}'") ?? [])}}})";
}

public class ApplianceModelAndParams : NatureRemoModel
{
    [JsonPropertyName("model")] public ApplianceModel? Model { get; set; }
    [JsonPropertyName("params")] public AirConParams? Params { get; set; }

    public override string ToString() =>
        $"ApplianceModelAndParams(model={Model}, params={Params})";
}

public class AirConRangeMode : NatureRemoModel
{
    [JsonPropertyName("temp")] public List<string>? Temp { get; set; }
    [JsonPropertyName("vol")] public List<string>? Vol { get; set; }
    [JsonPropertyName("dir")] public List<string>? Dir { get; set; }
    [JsonPropertyName("dirh")] public List<string>? Dirh { get; set; }

    public override string ToString() =>
    $"AirConRangeMode(temp=[{string.Join(", ", Temp ?? [])}], vol=[{string.Join(", ", Vol ?? [])}], " +
    $"dir=[{string.Join(", ", Dir ?? [])}], dirh=[{string.Join(", ", Dirh ?? [])}])";
}

public class AirConRange : NatureRemoModel
{
    [JsonPropertyName("modes")]
    public Dictionary<string, AirConRangeMode>? Modes { get; set; }

    [JsonPropertyName("fixedButtons")]
    public List<string>? FixedButtons { get; set; }
    [JsonPropertyName("extras")] public List<AirConExtra>? Extras { get; set; }

    public override string ToString() =>
        $"AirConRange(modes={{{string.Join(", ", Modes?.Select(kv => $"'{kv.Key}': {kv.Value}") ?? [])}}}, " +
        $"fixedButtons=[{string.Join(", ", FixedButtons ?? [])}], extras=[{string.Join(", ", Extras ?? [])}])";
}

public class AirCon : NatureRemoModel
{
    [JsonPropertyName("range")] public AirConRange? Range { get; set; }
    [JsonPropertyName("tempUnit")] public string TempUnit { get; set; } = "";

    public override string ToString() => $"AirCon(range={Range}, tempUnit=\"{TempUnit}\")";
}

public class Signal : NatureRemoModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("image")] public string Image { get; set; } = "";

    public override string ToString() => $"Signal(id='{Id}', name='{Name}', image='{Image}')";
}

public class Button : NatureRemoModel
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("image")] public string Image { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";

    public override string ToString() => $"Button(name='{Name}', image='{Image}', label='{Label}')";
}

public class TVState : NatureRemoModel
{
    [JsonPropertyName("input")] public string Input { get; set; } = "";

    public override string ToString() => $"TVState(input='{Input}')";
}

public class TV : NatureRemoModel
{
    [JsonPropertyName("state")] public TVState? State { get; set; }
    [JsonPropertyName("buttons")] public List<Button>? Buttons { get; set; }
    [JsonPropertyName("layout")] public List<TvRowLayout>? Layout { get; set; }

    public override string ToString() =>
        $"TV(state={State}, buttons=[{string.Join(", ", Buttons ?? [])}], layout=[{string.Join(", ", Layout ?? [])}])";
}

public class LightState : NatureRemoModel
{
    [JsonPropertyName("brightness")] public string Brightness { get; set; } = "";
    [JsonPropertyName("power")] public string Power { get; set; } = "";
    [JsonPropertyName("last_button")] public string LastButton { get; set; } = "";

    public override string ToString() =>
        $"LightState(brightness='{Brightness}', power='{Power}', last_button='{LastButton}')";
}

public class Light : NatureRemoModel
{
    [JsonPropertyName("state")] public LightState? State { get; set; }
    [JsonPropertyName("buttons")] public List<Button>? Buttons { get; set; }

    public override string ToString() =>
        $"Light(state={State}, buttons=[{string.Join(", ", Buttons ?? [])}])";
}

public class EchonetLiteProperty : NatureRemoModel
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("epc")] public int Epc { get; set; }
    [JsonPropertyName("val")] public string Val { get; set; } = "";
    [JsonPropertyName("updated_at")] public DateTimeOffset UpdatedAt { get; set; }

    public override string ToString() =>
        $"EchonetLiteProperty(name='{Name}', epc={Epc}, val='{Val}', updated_at={UpdatedAt:o})";
}

public class SmartMeter : NatureRemoModel
{
    [JsonPropertyName("echonetlite_properties")]
    public List<EchonetLiteProperty>? EchonetLiteProperties { get; set; }

    public override string ToString() =>
        $"SmartMeter(echonetlite_properties=[{string.Join(", ", EchonetLiteProperties ?? [])}])";
}

public class Appliance : NatureRemoModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("device")] public DeviceCore? Device { get; set; }
    [JsonPropertyName("model")] public ApplianceModel? Model { get; set; }
    [JsonPropertyName("nickname")] public string Nickname { get; set; } = "";
    [JsonPropertyName("image")] public string Image { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("settings")] public AirConParams? Settings { get; set; }
    [JsonPropertyName("aircon")] public AirCon? AirCon { get; set; }
    [JsonPropertyName("signals")] public List<Signal>? Signals { get; set; }
    [JsonPropertyName("tv")] public TV? Tv { get; set; }
    [JsonPropertyName("light")] public Light? Light { get; set; }
    [JsonPropertyName("smart_meter")] public SmartMeter? SmartMeter { get; set; }
    [JsonPropertyName("floor_heater")] public AirCon? FloorHeater { get; set; }
    [JsonPropertyName("echonetlite")] public EchonetLiteInfo? EchonetLite { get; set; }
    [JsonPropertyName("mornin_plus")] public MorninPlus? MorninPlus { get; set; }
    [JsonPropertyName("matter_bridged_device_state")] public string? MatterBridgedDeviceState { get; set; }
    [JsonPropertyName("ble")] public BleControlItem? Ble { get; set; }
    [JsonPropertyName("evcc")] public Evcc? Evcc { get; set; }
    [JsonPropertyName("light_projector")] public LightProjector? LightProjector { get; set; }
    [JsonPropertyName("qrio_lock")] public QrioLock? QrioLock { get; set; }

    public override string ToString() =>
        $"Appliance(id='{Id}', device={Device}, model={Model}, nickname='{Nickname}', " +
        $"image='{Image}', type='{Type}', settings={Settings}, aircon={AirCon}, " +
        $"signals=[{string.Join(", ", Signals ?? [])}], tv={Tv}, light={Light}, smart_meter={SmartMeter}, floor_heater={FloorHeater}, echonetlite={EchonetLite}, mornin_plus={MorninPlus}, matter_bridged_device_state={MatterBridgedDeviceState}, ble={Ble}, evcc={Evcc}, light_projector={LightProjector}, qrio_lock={QrioLock})";
}

public class IRSignal : NatureRemoModel
{
    [JsonPropertyName("freq")] public int Freq { get; set; }
    [JsonPropertyName("data")] public List<int> Data { get; set; } = [];
    [JsonPropertyName("format")] public string Format { get; set; } = "";

    public override string ToString() =>
        $"IRSignal(freq={Freq}, data=[{string.Join(", ", Data)}], format='{Format}')";
}

public class CommissioneeFabric : NatureRemoModel
{
    [JsonPropertyName("cfabric")] public string Cfabric { get; set; } = "";
    [JsonPropertyName("vendor_name")] public string VendorName { get; set; } = "";
    [JsonPropertyName("company_name")] public string CompanyName { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
}

public class ExtraOption : NatureRemoModel
{
    [JsonPropertyName("default")] public bool Default { get; set; }
    [JsonPropertyName("text")] public string Text { get; set; } = "";
    [JsonPropertyName("value")] public string Value { get; set; } = "";
}

public class AirConExtra : NatureRemoModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("text")] public string Text { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("availability")] public string Availability { get; set; } = "";
    [JsonPropertyName("defaultTime")] public string? DefaultTime { get; set; }
    [JsonPropertyName("options")] public List<ExtraOption>? Options { get; set; }
}

public class TvRowLayout : NatureRemoModel
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("buttons")] public List<string>? Buttons { get; set; }
}

public class ElManufacturer : NatureRemoModel
{
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

public class EchonetLiteInfo : NatureRemoModel
{
    [JsonPropertyName("identifier")] public string Identifier { get; set; } = "";
    [JsonPropertyName("instance")] public string Instance { get; set; } = "";
    [JsonPropertyName("ip")] public string? Ip { get; set; }
    [JsonPropertyName("manufacturer")] public ElManufacturer? Manufacturer { get; set; }
    [JsonPropertyName("route_type")] public string RouteType { get; set; } = "";
    [JsonPropertyName("version")] public string Version { get; set; } = "";
}

public class MorninPlusDevice : NatureRemoModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("image")] public string Image { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("active")] public bool Active { get; set; }
}

public class MorninPlus : NatureRemoModel
{
    [JsonPropertyName("devices")] public List<MorninPlusDevice?>? Devices { get; set; }
    [JsonPropertyName("hi_speed_mode")] public bool HiSpeedMode { get; set; }
}