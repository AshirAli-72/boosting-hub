using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoostingHub.backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PackagesController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly ILogger<PackagesController> _logger;

    public PackagesController(IPackageService packageService, ILogger<PackagesController> logger)
    {
        _packageService = packageService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive = null)
    {
        var packages = await _packageService.GetAllPackagesAsync(isActive);
        return Ok(packages);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var pkg = await _packageService.GetPackageByIdAsync(id);
        if (pkg == null) return NotFound(new { message = "Package not found." });
        return Ok(pkg);
    }

    [HttpGet("by-platform-service")]
    public async Task<IActionResult> GetByPlatformService([FromQuery] string platform, [FromQuery] string service)
    {
        if (string.IsNullOrWhiteSpace(platform) || string.IsNullOrWhiteSpace(service))
            return BadRequest(new { message = "Platform and service are required." });

        var packages = await _packageService.GetPackagesByPlatformServiceAsync(platform, service);
        return Ok(packages);
    }

    [HttpGet("platforms")]
    public async Task<IActionResult> GetPlatforms()
    {
        var platforms = await _packageService.GetPlatformsAsync();
        return Ok(platforms);
    }

    [HttpGet("services/{platform}")]
    public async Task<IActionResult> GetServices(string platform)
    {
        var services = await _packageService.GetServicesAsync(platform);
        return Ok(services);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Package pkg)
    {
        if (string.IsNullOrWhiteSpace(pkg.Platform) || string.IsNullOrWhiteSpace(pkg.Service))
            return BadRequest(new { message = "Platform and Service are required." });

        var allPackages = await _packageService.GetAllPackagesAsync();
        if (allPackages.Any(p => p.Platform == pkg.Platform && p.Service == pkg.Service))
            return Conflict(new { message = $"A package for \"{pkg.Platform}\" — \"{pkg.Service}\" already exists." });

        var created = await _packageService.CreatePackageAsync(pkg);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Package pkg)
    {
        var updated = await _packageService.UpdatePackageAsync(id, pkg);
        if (updated == null) return NotFound(new { message = "Package not found." });
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _packageService.DeletePackageAsync(id);
        if (!deleted) return NotFound(new { message = "Package not found." });
        return Ok(new { message = "Package deleted successfully." });
    }

    [HttpGet("all-for-admin")]
    public async Task<IActionResult> GetAllForAdmin()
    {
        var packages = await _packageService.GetAllPackagesAsync();
        return Ok(packages);
    }
}
