using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace BoostingHub.backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeoLocationController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, (string Currency, DateTime Expiry)> _cache = new();
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);

    private static readonly string[] _fallbackApis = new[]
    {
        "https://ipapi.co/{ip}/currency/",
        "https://ipwho.io/{ip}",
    };

    [HttpGet("currency")]
    public async Task<IActionResult> GetCurrency()
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (IsLocal(clientIp))
            return Ok(new { currency = "USD" });

        if (_cache.TryGetValue(clientIp!, out var cached) && cached.Expiry > DateTime.UtcNow)
            return Ok(new { currency = cached.Currency });

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        try
        {
            var resp = await http.GetAsync($"http://ip-api.com/json/{clientIp}?fields=status,currency,countryCode");
            if (resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var json = JsonSerializer.Deserialize<JsonElement>(content);
                    if (json.TryGetProperty("status", out var st) && st.GetString() == "success"
                        && json.TryGetProperty("currency", out var cur) && cur.ValueKind == JsonValueKind.String)
                    {
                        var currency = cur.GetString();
                        if (!string.IsNullOrEmpty(currency) && currency.Length == 3)
                        {
                            _cache[clientIp!] = (currency, DateTime.UtcNow.Add(_cacheDuration));
                            return Ok(new { currency });
                        }
                    }
                }
            }
        }
        catch { }

        foreach (var api in _fallbackApis)
        {
            try
            {
                var url = api.Replace("{ip}", clientIp);
                using var fbHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                fbHttp.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                var resp = await fbHttp.GetAsync(url);
                if (resp.IsSuccessStatusCode)
                {
                    var content = (await resp.Content.ReadAsStringAsync()).Trim().Trim('"');
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var currency = ExtractCurrency(content, api);
                        if (!string.IsNullOrEmpty(currency) && currency.Length == 3)
                        {
                            _cache[clientIp!] = (currency, DateTime.UtcNow.Add(_cacheDuration));
                            return Ok(new { currency });
                        }
                    }
                }
            }
            catch { }
        }

        _cache[clientIp!] = ("USD", DateTime.UtcNow.Add(_cacheDuration));
        return Ok(new { currency = "USD" });
    }

    private static bool IsLocal(string? ip)
        => string.IsNullOrEmpty(ip) || ip == "::1" || ip == "127.0.0.1" || ip == "::ffff:127.0.0.1";

    private static string? ExtractCurrency(string content, string apiUrl)
    {
        if (apiUrl.Contains("ipapi.co"))
        {
            if (content.Length == 3 && content.All(char.IsLetter))
                return content.ToUpperInvariant();
        }

        if (apiUrl.Contains("ipwho.io"))
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(content);
                if (json.TryGetProperty("currency", out var cur) && cur.TryGetProperty("code", out var code))
                    return code.GetString();
            }
            catch { }
        }

        return null;
    }
}
