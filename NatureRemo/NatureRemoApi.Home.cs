namespace NatureRemo;

// Home management and invite endpoints that were missing from the other partial files.
// Requires `public partial class NatureRemoApi` in NatureRemoApi.cs.
//
// NOTE: These methods follow the official OpenAPI specification and have not been verified
// against a real account. Mutating calls (delete / kick / owner / part / transfer) are destructive.
public partial class NatureRemoApi
{
    private static IEnumerable<KeyValuePair<string, string>>? ToForm(IReadOnlyDictionary<string, string>? parameters) => parameters?.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value));

    // ---- homes ------------------------------------------------------------

    /// <summary>Create a new home (requires the "home.write" scope).</summary>
    /// <param name="name">Home name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created home.</returns>
    public Task<Home> CreateHomeAsync(string name, CancellationToken ct = default) => PostJsonAsync<Home>("/1/homes", Form(("name", name)), ct);

    /// <summary>Update a home (requires the "home.write" scope).</summary>
    /// <param name="home">Home ID.</param>
    /// <param name="name">New home name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated home.</returns>
    public Task<Home> UpdateHomeAsync(string home, string name, CancellationToken ct = default) => PostJsonAsync<Home>($"/1/homes/{home}", Form(("name", name)), ct);

    /// <summary>Delete a home (requires the "home.write" scope).</summary>
    /// <param name="home">Home ID.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task DeleteHomeAsync(string home, CancellationToken ct = default) => PostAsync($"/1/homes/{home}/delete", null, ct);

    /// <summary>Part (leave) from a home (requires the "home.write" scope).</summary>
    /// <param name="home">Home ID.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task PartHomeAsync(string home, CancellationToken ct = default) => PostAsync($"/1/homes/{home}/part", null, ct);

    /// <summary>Change the owner of a home (requires the "home.write" scope).</summary>
    /// <param name="home">Home ID.</param>
    /// <param name="user">User ID of the new owner.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated home.</returns>
    public Task<Home> ChangeHomeOwnerAsync(string home, string user, CancellationToken ct = default) => PostJsonAsync<Home>($"/1/homes/{home}/owner", Form(("user", user)), ct);

    /// <summary>Kick a user from a home (requires the "home.write" scope).</summary>
    /// <param name="home">Home ID.</param>
    /// <param name="user">User ID to kick.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task KickHomeUserAsync(string home, string user, CancellationToken ct = default) => PostAsync($"/1/homes/{home}/kick", Form(("user", user)), ct);

    /// <summary>Transfer devices to another home (requires the "home.write" scope).</summary>
    /// <param name="home">Source home ID.</param>
    /// <param name="toHome">Destination home ID.</param>
    /// <param name="devices">Device IDs to transfer.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task TransferHomeDevicesAsync(
        string home, string toHome, IEnumerable<string> devices, CancellationToken ct = default) => PostAsync($"/1/homes/{home}/transfer/{toHome}", Form(("devices", string.Join(",", devices))), ct);

    // ---- home location ----------------------------------------------------

    /// <summary>
    /// Update a home's location (requires the "home.write" scope).
    /// The OpenAPI specification does not describe the request parameters of this endpoint,
    /// so they are passed through as-is as form fields.
    /// </summary>
    /// <param name="home">Home ID.</param>
    /// <param name="parameters">Form fields to send (optional).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated home.</returns>
    public Task<Home> UpdateHomeLocationAsync(
        string home, IReadOnlyDictionary<string, string>? parameters = null, CancellationToken ct = default) => PostJsonAsync<Home>($"/1/homes/{home}/location", ToForm(parameters), ct);

    /// <summary>Delete a home's location (requires the "home.write" scope).</summary>
    /// <param name="home">Home ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated home.</returns>
    public Task<Home> DeleteHomeLocationAsync(string home, CancellationToken ct = default) => PostJsonAsync<Home>($"/1/homes/{home}/location/delete", null, ct);

    /// <summary>
    /// Update the user's location state for a home (requires the "home.write" scope).
    /// The OpenAPI specification does not describe the request parameters of this endpoint,
    /// so they are passed through as-is as form fields.
    /// </summary>
    /// <param name="home">Home ID.</param>
    /// <param name="parameters">Form fields to send (optional).</param>
    /// <param name="ct">Cancellation token.</param>
    public Task UpdateHomeLocationStateAsync(
        string home, IReadOnlyDictionary<string, string>? parameters = null, CancellationToken ct = default) => PostAsync($"/1/homes/{home}/location_state/update", ToForm(parameters), ct);

    // ---- invites ----------------------------------------------------------

    /// <summary>Create a new home invite (requires the "home.write" scope).</summary>
    /// <param name="home">Home ID.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<HomeInvite> CreateHomeInviteAsync(string home, CancellationToken ct = default) => PostJsonAsync<HomeInvite>($"/1/homes/{home}/invites", null, ct);

    /// <summary>Show a home invite (requires the "home.read" scope).</summary>
    /// <param name="inviteToken">Invite token.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<HomeInvite> GetHomeInviteAsync(string inviteToken, CancellationToken ct = default) => FetchAsync<HomeInvite>($"/1/invites/{inviteToken}", ct);

    /// <summary>Accept a home invite (requires the "home.write" scope).</summary>
    /// <param name="inviteToken">Invite token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The home that was joined.</returns>
    public Task<Home> AcceptHomeInviteAsync(string inviteToken, CancellationToken ct = default) => PostJsonAsync<Home>($"/1/invites/{inviteToken}", null, ct);
}
