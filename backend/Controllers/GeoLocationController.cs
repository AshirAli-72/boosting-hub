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
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        try
        {
            var apiUrl = "http://ip-api.com/json/?fields=status,currency,countryCode";
            if (!string.IsNullOrEmpty(clientIp) && clientIp != "::1" && clientIp != "127.0.0.1" && clientIp != "::ffff:127.0.0.1")
            {
                apiUrl = $"http://ip-api.com/json/{clientIp}?fields=status,currency,countryCode";
            }

            var resp = await http.GetAsync(apiUrl);
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
                            return Ok(new { currency });
                    }
                }
            }
        }
        catch { }

        return Ok(new { currency = "USD" });
    }
}
