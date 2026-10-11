// Models for the control endpoints in NatureRemoApi.Control.cs.
// They mirror the JSON of the Nature Remo API one-to-one, so missing-XML-doc
// warnings are suppressed here, as in Models.cs.
#pragma warning disable CS1591

using System.Text.Json.Serialization;

namespace NatureRemo;

/// <summary>Relay state to request for a Nature EV Switch.</summary>
public enum EvccRelayState
{
    Open,
    Close,
}

// ---- power distribution board ---------------------------------------------

public class PowerDistBoardChannel : NatureRemoModel
{
    /// <summary>Channel number (1-252).</summary>
    [JsonPropertyName("number")] public int Number { get; set; }

    /// <summary>Room name (max 40 chars).</summary>
    [JsonPropertyName("room_name")] public string RoomName { get; set; } = "";

    /// <summary>Appliance name (max 40 chars).</summary>
    [JsonPropertyName("appliance_name")] public string ApplianceName { get; set; } = "";

    public override string ToString() =>
        $"PowerDistBoardChannel(number={Number}, room_name='{RoomName}', appliance_name='{ApplianceName}')";
}

public class PowerDistBoardOptions : NatureRemoModel
{
    [JsonPropertyName("appliance_names")] public List<string>? ApplianceNames { get; set; }
    [JsonPropertyName("room_names")] public List<string>? RoomNames { get; set; }

    public override string ToString() =>
        $"PowerDistBoardOptions(appliance_names=[{string.Join(", ", ApplianceNames ?? [])}], " +
        $"room_names=[{string.Join(", ", RoomNames ?? [])}])";
}

public class PowerDistBoard : NatureRemoModel
{
    [JsonPropertyName("channels")] public List<PowerDistBoardChannel>? Channels { get; set; }

    public override string ToString() =>
        $"PowerDistBoard(channels=[{string.Join(", ", Channels ?? [])}])";
}

public class PowerDistBoardDashboard : NatureRemoModel
{
    [JsonPropertyName("options")] public PowerDistBoardOptions? Options { get; set; }
    [JsonPropertyName("power_dist_board")] public PowerDistBoard? Board { get; set; }

    public override string ToString() => $"PowerDistBoardDashboard(options={Options}, board={Board})";
}

// ---- BLE ------------------------------------------------------------------

public class BlePrivateMacro : NatureRemoModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("url")] public string Url { get; set; } = "";

    public override string ToString() => $"BlePrivateMacro(id='{Id}', url='{Url}')";
}

// ---- appliance detection (v2) ---------------------------------------------

public class DetectApplianceV2Response : NatureRemoModel
{
    [JsonPropertyName("results")] public List<ApplianceModelAndParams>? Results { get; set; }

    /// <summary>Pass this back to the next detection call to narrow down the candidates.</summary>
    [JsonPropertyName("session_token")] public string SessionToken { get; set; } = "";

    [JsonPropertyName("suggested_action")] public string SuggestedAction { get; set; } = "";
    [JsonPropertyName("suggested_action_message")] public string SuggestedActionMessage { get; set; } = "";

    public override string ToString() =>
        $"DetectApplianceV2Response(results=[{string.Join(", ", Results ?? [])}], " +
        $"session_token='{SessionToken}', suggested_action='{SuggestedAction}', " +
        $"suggested_action_message='{SuggestedActionMessage}')";
}
