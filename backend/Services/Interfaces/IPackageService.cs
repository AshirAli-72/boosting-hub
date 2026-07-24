using BoostingHub.backend.DTOs;
using BoostingHub.backend.Models;

namespace BoostingHub.backend.Services.Interfaces;

public interface IPackageService
{
    Task<List<Package>> GetAllPackagesAsync(bool? isActive = null, CancellationToken ct = default);
    Task<List<Package>> GetPackagesByPlatformServiceAsync(string platform, string service, CancellationToken ct = default);
    Task<Package?> GetPackageByIdAsync(int id, CancellationToken ct = default);
    Task<Package> CreatePackageAsync(Package pkg, CancellationToken ct = default);
    Task<Package?> UpdatePackageAsync(int id, Package pkg, CancellationToken ct = default);
    Task<bool> DeletePackageAsync(int id, CancellationToken ct = default);
    Task<List<string>> GetPlatformsAsync(CancellationToken ct = default);
    Task<List<string>> GetServicesAsync(string platform, CancellationToken ct = default);
}
