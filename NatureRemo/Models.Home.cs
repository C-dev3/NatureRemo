// Models for the home / invite endpoints in NatureRemoApi.Home.cs.
// They mirror the JSON of the Nature Remo API one-to-one, so missing-XML-doc
// warnings are suppressed here, as in Models.cs.
#pragma warning disable CS1591

using System.Text.Json.Serialization;

namespace NatureRemo;

public class HomeInvite : NatureRemoModel
{
    /// <summary>The home being invited to (<c>users</c> is not included in this response).</summary>
    [JsonPropertyName("home")] public Home? Home { get; set; }

    [JsonPropertyName("url")] public string Url { get; set; } = "";

    /// <summary>The user who created the invite.</summary>
    [JsonPropertyName("user")] public User? User { get; set; }

    public override string ToString() => $"HomeInvite(home={Home}, url='{Url}', user={User})";
}
