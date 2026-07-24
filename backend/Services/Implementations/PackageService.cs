using BoostingHub.backend.Data;
using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Services.Implementations;

public class PackageService : IPackageService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PackageService> _logger;

    public PackageService(ApplicationDbContext db, ILogger<PackageService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Package>> GetAllPackagesAsync(bool? isActive = null, CancellationToken ct = default)
    {
        var query = _db.Packages.AsNoTracking();
        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);
        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
    }

    public async Task<List<Package>> GetPackagesByPlatformServiceAsync(string platform, string service, CancellationToken ct = default)
    {
        return await _db.Packages
            .AsNoTracking()
            .Where(p => p.IsActive && p.Platform == platform && p.Service == service)
            .OrderBy(p => p.Price)
            .ToListAsync(ct);
    }

    public async Task<Package?> GetPackageByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Packages.FindAsync(new object[] { id }, ct);
    }

    public async Task<Package> CreatePackageAsync(Package pkg, CancellationToken ct = default)
    {
        pkg.CreatedAt = DateTime.UtcNow;
        _db.Packages.Add(pkg);
        await _db.SaveChangesAsync(ct);
        return pkg;
    }

    public async Task<Package?> UpdatePackageAsync(int id, Package pkg, CancellationToken ct = default)
    {
        var existing = await _db.Packages.FindAsync(new object[] { id }, ct);
        if (existing == null) return null;

        existing.Platform = pkg.Platform;
        existing.Service = pkg.Service;
        existing.Price = pkg.Price;
        existing.IsActive = pkg.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeletePackageAsync(int id, CancellationToken ct = default)
    {
        var pkg = await _db.Packages.FindAsync(new object[] { id }, ct);
        if (pkg == null) return false;

        _db.Packages.Remove(pkg);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<string>> GetPlatformsAsync(CancellationToken ct = default)
    {
        return await _db.Packages
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => p.Platform)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync(ct);
    }

    public async Task<List<string>> GetServicesAsync(string platform, CancellationToken ct = default)
    {
        return await _db.Packages
            .AsNoTracking()
            .Where(p => p.IsActive && p.Platform == platform)
            .Select(p => p.Service)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync(ct);
    }
}
