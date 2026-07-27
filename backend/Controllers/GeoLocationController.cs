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

    [HttpGet("currency")]
    public async Task<IActionResult> GetCurrency()
    {
        try
        {
            var resp = await _http.GetAsync("http://ip-api.com/json/?fields=status,currency,countryCode");
            if (!resp.IsSuccessStatusCode)
                return Ok(new { currency = "USD" });

            var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            if (json != null && json.TryGetValue("status", out var status) && status == "success"
                && json.TryGetValue("currency", out var currency) && !string.IsNullOrEmpty(currency))
            {
                return Ok(new { currency });
            }
        }
        catch { }

        return Ok(new { currency = "USD" });
    }
}
