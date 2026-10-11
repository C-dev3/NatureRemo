using System.Text;
using System.Text.Json;

namespace NatureRemo;

/// <summary>Client for the Nature Remo Local API.</summary>
/// <remarks>Creates a new local API client.</remarks>
/// <param name="addr">IP address (or host name) of Remo.</param>
/// <param name="httpClient">
/// Optional <see cref="HttpClient"/>. If omitted, one is created and disposed by this instance.
/// </param>
public class NatureRemoLocalApi(string addr, HttpClient? httpClient = null) : IDisposable
{
    private const string Version = "0.3.3";
    private const string ProjectUrl = "https://github.com/morinokami/nature-remo";

    private readonly HttpClient _http = httpClient ?? new HttpClient();
    private readonly bool _ownsClient = httpClient is null;

    /// <summary>Address (IP or host name) of Remo.</summary>
    public string Addr { get; } = addr;

    /// <summary>Disposes the internal <see cref="HttpClient"/> if this instance created it.</summary>
    public void Dispose()
    {
        if (_ownsClient) _http.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<HttpResponseMessage> RequestAsync(
        HttpMethod method, string? body, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, $"http://{Addr}/messages");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("X-Requested-With", $"nature-remo/{Version} ({ProjectUrl})");

        if (method == HttpMethod.Post)
            request.Content = new StringContent(body ?? "", Encoding.UTF8);

        try
        {
            return await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            throw new NatureRemoException(e.Message, e);
        }
    }

    /// <summary>Fetch the newest received IR signal.</summary>
    public async Task<IRSignal> GetIRSignalAsync(CancellationToken ct = default)
    {
        using var resp = await RequestAsync(HttpMethod.Get, null, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            throw new NatureRemoException($"{(int)resp.StatusCode} {resp.ReasonPhrase}");

        var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var result = await JsonSerializer.DeserializeAsync<IRSignal>(stream, Json.Options, ct)
            .ConfigureAwait(false);
        return result ?? throw new NatureRemoException("Empty response body");
    }

    /// <summary>
    /// Emit IR signals provided by request body.
    /// <paramref name="message"/> is a JSON string with "data", "freq" and "format" keys.
    /// </summary>
    public async Task SendIRSignalAsync(string message, CancellationToken ct = default)
    {
        using var resp = await RequestAsync(HttpMethod.Post, message, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            throw new NatureRemoException($"{(int)resp.StatusCode} {resp.ReasonPhrase}");
    }
}