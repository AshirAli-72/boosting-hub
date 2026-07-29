using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;

namespace BoostingHub.backend.Services.Interfaces;

public interface ISiteSettingService
{
    Task<Result<SiteSettingDto>> GetAsync();
    Task<Result> UpdateAsync(SiteSettingDto dto);
}
