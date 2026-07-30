using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;

namespace BoostingHub.backend.Services.Interfaces;

public interface IWebsiteSettingService
{
    Task<Result<WebsiteSettingDto>> GetAsync();
    Task<Result> UpdateAsync(WebsiteSettingDto dto);
}
