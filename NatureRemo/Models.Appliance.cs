// Typed models for appliance sub-objects (BLE, EVCC, light projector, Qrio lock) and
// other fields that used to be missing or left as raw JSON. They mirror the JSON of the
// Nature Remo API one-to-one, so missing-XML-doc warnings are suppressed here, as in Models.cs.
#pragma warning disable CS1591

using System.Text.Json.Serialization;

namespace NatureRemo;

// ---- generic --------------------------------------------------------------

public class ValueUpdatedAt<T> : NatureRemoModel
{
    [JsonPropertyName("value")] public T? Value { get; set; }
    [JsonPropertyName("updated_at")] public DateTimeOffset UpdatedAt { get; set; }

    public override string ToString() => $"ValueUpdatedAt(value={Value}, updated_at={UpdatedAt:o})";
}

// ---- user / device --------------------------------------------------------

public class LinkText : NatureRemoModel
{
    [JsonPropertyName("text")] public string Text { get; set; } = "";
    [JsonPropertyName("link")] public string Link { get; set; } = "";

    public override string ToString() => $"LinkText(text='{Text}', link='{Link}')";
}

public class PrivacyPolicyRevisionOutline : NatureRemoModel
{
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("body")] public string Body { get; set; } = "";
    [JsonPropertyName("links")] public Dictionary<string, LinkText>? Links { get; set; }

    public override string ToString() =>
        $"PrivacyPolicyRevisionOutline(title='{Title}', body='{Body}', " +
        $"links={{{string.Join(", ", Links?.Select(kv => $"'{kv.Key}': {kv.Value}") ?? [])}}})";
}

/// <summary>Deprecated: a user of a device, as returned in <see cref="Device.Users"/>.</summary>
public class UserDevice : NatureRemoModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("nickname")] public string Nickname { get; set; } = "";
    [JsonPropertyName("superuser")] public bool Superuser { get; set; }

    public override string ToString() => $"UserDevice(id='{Id}', nickname='{Nickname}', superuser={Superuser})";
}

// ---- BLE ------------------------------------------------------------------

public class BleHueLightControlItem : NatureRemoModel
{
    [JsonPropertyName("light_type")] public string LightType { get; set; } = "";

    public override string ToString() => $"BleHueLightControlItem(light_type='{LightType}')";
}

public class BleSesameControlItem : NatureRemoModel
{
    [JsonPropertyName("uuid")] public string Uuid { get; set; } = "";
    [JsonPropertyName("device_type")] public string DeviceType { get; set; } = "";
    [JsonPropertyName("key_level")] public string KeyLevel { get; set; } = "";
    [JsonPropertyName("user_index")] public List<int>? UserIndex { get; set; }

    public override string ToString() =>
        $"BleSesameControlItem(uuid='{Uuid}', device_type='{DeviceType}', key_level='{KeyLevel}', " +
        $"user_index=[{string.Join(", ", UserIndex ?? [])}])";
}

public class BleControlItem : NatureRemoModel
{
    [JsonPropertyName("addr")] public string Addr { get; set; } = "";
    [JsonPropertyName("addr_type")] public string AddrType { get; set; } = "";
    [JsonPropertyName("bonded")] public bool Bonded { get; set; }
    [JsonPropertyName("hue")] public BleHueLightControlItem? Hue { get; set; }
    [JsonPropertyName("macros")] public Dictionary<string, BlePrivateMacro>? Macros { get; set; }
    [JsonPropertyName("sesame")] public BleSesameControlItem? Sesame { get; set; }

    public override string ToString() =>
        $"BleControlItem(addr='{Addr}', addr_type='{AddrType}', bonded={Bonded}, hue={Hue}, " +
        $"macros={{{string.Join(", ", Macros?.Select(kv => $"'{kv.Key}': {kv.Value}") ?? [])}}}, sesame={Sesame})";
}

// ---- Nature EV Switch -----------------------------------------------------

/// <summary>
/// An error / warning entry. <c>kind</c> is one of relay, meter, meter_communication, memory,
/// unrecoverable (self test) or abnormal_temperature, warnings (auto recovering); <c>type</c>
/// is the kind-specific detail such as "over_temperature" or "unknown".
/// </summary>
public class EvccStateErrorValue : NatureRemoModel
{
    [JsonPropertyName("kind")] public string Kind { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";

    public override string ToString() => $"EvccStateErrorValue(kind='{Kind}', type='{Type}')";
}

public class EvccStateErrors : NatureRemoModel
{
    [JsonPropertyName("require_self_test")]
    public List<ValueUpdatedAt<EvccStateErrorValue>>? RequireSelfTest { get; set; }

    [JsonPropertyName("auto_recovering")]
    public List<ValueUpdatedAt<EvccStateErrorValue>>? AutoRecovering { get; set; }

    public override string ToString() =>
        $"EvccStateErrors(require_self_test=[{string.Join(", ", RequireSelfTest ?? [])}], " +
        $"auto_recovering=[{string.Join(", ", AutoRecovering ?? [])}])";
}

public class VehicleDetail : NatureRemoModel
{
    [JsonPropertyName("battery_capacity")] public double? BatteryCapacity { get; set; }

    /// <summary>black, grey, white, red, blue, light_blue, green or beige.</summary>
    [JsonPropertyName("body_color")] public string BodyColor { get; set; } = "";

    /// <summary>kei, compact, hatchback, minivan, sedan, suv or coupe.</summary>
    [JsonPropertyName("body_type")] public string BodyType { get; set; } = "";

    [JsonPropertyName("model")] public string? Model { get; set; }
    [JsonPropertyName("model_year")] public int? ModelYear { get; set; }

    public override string ToString() =>
        $"VehicleDetail(battery_capacity={BatteryCapacity?.ToString() ?? "null"}, body_color='{BodyColor}', " +
        $"body_type='{BodyType}', model='{Model}', model_year={ModelYear?.ToString() ?? "null"})";
}

public class Vehicle : NatureRemoModel
{
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("detail")] public VehicleDetail? Detail { get; set; }

    public override string ToString() => $"Vehicle(manufacturer='{Manufacturer}', detail={Detail})";
}

public class Evcc : NatureRemoModel
{
    [JsonPropertyName("connected_device")] public Device? ConnectedDevice { get; set; }
    [JsonPropertyName("current_charging_amount")] public double? CurrentChargingAmount { get; set; }
    [JsonPropertyName("errors")] public EvccStateErrors? Errors { get; set; }
    [JsonPropertyName("last_operated_remotely")] public bool LastOperatedRemotely { get; set; }
    [JsonPropertyName("online")] public ValueUpdatedAt<bool>? Online { get; set; }

    /// <summary>"open" or "close".</summary>
    [JsonPropertyName("relay_state")] public ValueUpdatedAt<string>? RelayState { get; set; }

    [JsonPropertyName("vehicle")] public Vehicle? Vehicle { get; set; }

    public override string ToString() =>
        $"Evcc(connected_device={ConnectedDevice}, current_charging_amount={CurrentChargingAmount?.ToString() ?? "null"}, " +
        $"errors={Errors}, last_operated_remotely={LastOperatedRemotely}, online={Online}, " +
        $"relay_state={RelayState}, vehicle={Vehicle})";
}

// ---- light projector ------------------------------------------------------

public class LightProjectorTemplate : NatureRemoModel
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("uuid")] public string Uuid { get; set; } = "";
    [JsonPropertyName("image")] public string Image { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("text")] public string Text { get; set; } = "";
    [JsonPropertyName("x_size")] public int XSize { get; set; }
    [JsonPropertyName("y_size")] public int YSize { get; set; }
    [JsonPropertyName("templates")] public List<LightProjectorTemplate?>? Templates { get; set; }

    public override string ToString() =>
        $"{GetType().Name}(type='{Type}', name='{Name}', uuid='{Uuid}', image='{Image}', label='{Label}', " +
        $"text='{Text}', x_size={XSize}, y_size={YSize}, templates=[{string.Join(", ", Templates ?? [])}])";
}

public class LightProjectorLayout : LightProjectorTemplate { }

public class LightProjector : NatureRemoModel
{
    [JsonPropertyName("layout")] public LightProjectorLayout? Layout { get; set; }

    public override string ToString() => $"LightProjector(layout={Layout})";
}

// ---- Qrio lock ------------------------------------------------------------

public class QrioLockDevice : NatureRemoModel
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("image")] public string Image { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    public override string ToString() => $"QrioLockDevice(id={Id}, image='{Image}', name='{Name}')";
}

public class QrioLock : NatureRemoModel
{
    [JsonPropertyName("bd_address")] public string BdAddress { get; set; } = "";
    [JsonPropertyName("device")] public QrioLockDevice? Device { get; set; }
    [JsonPropertyName("is_available")] public bool IsAvailable { get; set; }
    [JsonPropertyName("sub_bd_address")] public string? SubBdAddress { get; set; }

    public override string ToString() =>
        $"QrioLock(bd_address='{BdAddress}', device={Device}, is_available={IsAvailable}, sub_bd_address='{SubBdAddress}')";
}
