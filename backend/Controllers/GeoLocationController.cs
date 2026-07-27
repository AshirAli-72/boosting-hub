using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace BoostingHub.backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeoLocationController : ControllerBase
{
    private static readonly HttpClient _http;

    static GeoLocationController()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
        };
        _http = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
    }

    [HttpGet("currency")]
    public async Task<IActionResult> GetCurrency()
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var apis = new (string url, Func<JsonElement, string?> extract)[]
        {
            ("https://ipapi.co/json/", json =>
            {
                if (json.TryGetProperty("currency", out var el) && el.ValueKind == JsonValueKind.String)
                    return el.GetString();
                return null;
            }),
            ("https://ipwho.is/", json =>
            {
                if (json.TryGetProperty("currency", out var el) && el.ValueKind == JsonValueKind.Object
                    && el.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String)
                    return code.GetString();
                return null;
            }),
            ("http://ip-api.com/json/?fields=status,currency,countryCode", json =>
            {
                if (json.TryGetProperty("status", out var st) && st.GetString() == "success"
                    && json.TryGetProperty("currency", out var el) && el.ValueKind == JsonValueKind.String)
                    return el.GetString();
                return null;
            })
        };

        foreach (var (url, extract) in apis)
        {
            try
            {
                var apiUrl = url;
                if (!string.IsNullOrEmpty(clientIp) && clientIp != "::1" && clientIp != "127.0.0.1")
                    apiUrl = url.Replace("/json/", $"/json/{clientIp}/").Replace("json/?", $"json/{clientIp}?");
                else if (url.Contains("ip-api.com"))
                    apiUrl = url;

                var resp = await _http.GetAsync(apiUrl);
                if (!resp.IsSuccessStatusCode) continue;

                var content = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content)) continue;

                var json = JsonSerializer.Deserialize<JsonElement>(content);
                var currency = extract(json);

                if (!string.IsNullOrEmpty(currency) && currency.Length == 3)
                    return Ok(new { currency });
            }
            catch { continue; }
        }

        return Ok(new { currency = "USD" });
    }
}
