using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace BoostingHub.backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeoLocationController : ControllerBase
{
    [HttpGet("currency")]
    public async Task<IActionResult> GetCurrency()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        var apis = new[]
        {
            "http://ip-api.com/json/?fields=status,currency,countryCode",
            "http://www.geoplugin.net/json.gp?ip=",
            "http://ip-api.com/json/"
        };

        foreach (var url in apis)
        {
            try
            {
                var resp = await http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) continue;

                var content = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content)) continue;

                var json = JsonSerializer.Deserialize<JsonElement>(content);

                string? currency = null;

                if (url.Contains("ip-api.com"))
                {
                    if (json.TryGetProperty("status", out var st) && st.GetString() == "success"
                        && json.TryGetProperty("currency", out var el) && el.ValueKind == JsonValueKind.String)
                        currency = el.GetString();
                }
                else if (url.Contains("geoplugin"))
                {
                    if (json.TryGetProperty("currencyCode", out var el) && el.ValueKind == JsonValueKind.String)
                        currency = el.GetString();
                }

                if (!string.IsNullOrEmpty(currency) && currency.Length == 3)
                    return Ok(new { currency });
            }
            catch { continue; }
        }

        return Ok(new { currency = "USD" });
    }
}
