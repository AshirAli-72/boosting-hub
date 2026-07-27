using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace BoostingHub.backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeoLocationController : ControllerBase
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private static readonly string[] _apiUrls = new[]
    {
        "https://ipapi.co/json/",
        "https://ipwho.is/",
        "https://ip-api.com/json/?fields=status,currency,countryCode"
    };

    [HttpGet("currency")]
    public async Task<IActionResult> GetCurrency()
    {
        foreach (var url in _apiUrls)
        {
            try
            {
                var resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) continue;

                var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                if (json == null) continue;

                string? currency = null;

                if (url.Contains("ipapi.co"))
                {
                    if (json.TryGetValue("currency", out var cur) && cur is JsonElement el && el.ValueKind == JsonValueKind.String)
                        currency = el.GetString();
                }
                else if (url.Contains("ipwho.is"))
                {
                    if (json.TryGetValue("currency", out var curObj) && curObj is JsonElement el && el.ValueKind == JsonValueKind.Object)
                    {
                        if (el.TryGetProperty("code", out var codeEl) && codeEl.ValueKind == JsonValueKind.String)
                            currency = codeEl.GetString();
                    }
                }
                else if (url.Contains("ip-api.com"))
                {
                    if (json.TryGetValue("status", out var st) && st is JsonElement stEl && stEl.GetString() == "success"
                        && json.TryGetValue("currency", out var cur) && cur is JsonElement curEl && curEl.ValueKind == JsonValueKind.String)
                        currency = curEl.GetString();
                }

                if (!string.IsNullOrEmpty(currency))
                    return Ok(new { currency });
            }
            catch { }
        }

        return Ok(new { currency = "USD" });
    }
}
