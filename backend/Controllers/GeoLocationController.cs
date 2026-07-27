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
            })
        };

        foreach (var (url, extract) in apis)
        {
            try
            {
                var resp = await http.GetAsync(url);
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
