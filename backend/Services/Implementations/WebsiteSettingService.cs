using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace BoostingHub.backend.Services.Implementations;

public class WebsiteSettingService : IWebsiteSettingService
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IMemoryCache _cache;

    private const string CacheKey = "website_settings";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private const string LogoDir = "uploads\\site\\logo";
    private const string FaviconDir = "uploads\\site\\favicon";

    public WebsiteSettingService(ApplicationDbContext db, IWebHostEnvironment env, IMemoryCache cache)
    {
        _db = db;
        _env = env;
        _cache = cache;
    }

    public async Task<Result<WebsiteSettingDto>> GetAsync()
    {
        if (_cache.TryGetValue(CacheKey, out WebsiteSettingDto? cached) && cached != null)
            return Result<WebsiteSettingDto>.Success(cached);

        try
        {
            var setting = await _db.WebsiteSettings.AsNoTracking().FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new WebsiteSetting();
                _db.WebsiteSettings.Add(setting);
                await _db.SaveChangesAsync();
            }

            var dto = MapToDto(setting);
            _cache.Set(CacheKey, dto, CacheDuration);
            return Result<WebsiteSettingDto>.Success(dto);
        }
        catch (Exception)
        {
            var fallback = new WebsiteSettingDto();
            return Result<WebsiteSettingDto>.Success(fallback);
        }
    }

    public async Task<Result> UpdateAsync(WebsiteSettingDto dto)
    {
        var setting = await _db.WebsiteSettings.FirstOrDefaultAsync();
        if (setting == null)
        {
            setting = new WebsiteSetting();
            _db.WebsiteSettings.Add(setting);
        }

        setting.SiteName = dto.SiteName ?? "Boosting Hub";
        setting.LogoPath = SaveFile(dto.LogoFile, LogoDir, setting.LogoPath, "logo");
        setting.FaviconPath = SaveFile(dto.FaviconFile, FaviconDir, setting.FaviconPath, "favicon");
        setting.HeroTitle = dto.HeroTitle;
        setting.HeroSubtitle = dto.HeroSubtitle;
        setting.HeroDescription = dto.HeroDescription;
        setting.AboutTitle = dto.AboutTitle;
        setting.AboutDescription = dto.AboutDescription;
        setting.SupportEmail = dto.SupportEmail;
        setting.SupportPhone = dto.SupportPhone;
        setting.Address = dto.Address;
        setting.FooterText = dto.FooterText;
        setting.FooterDescription = dto.FooterDescription;
        setting.TwitterUrl = dto.TwitterUrl;
        setting.LinkedInUrl = dto.LinkedInUrl;
        setting.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
        return Result.Success("Website settings saved successfully.");
    }

    private string? SaveFile(IFormFile? file, string subDir, string? currentPath, string prefix)
    {
        if (file is not { Length: > 0 }) return currentPath;

        var uploadsDir = Path.Combine(_env.WebRootPath, subDir);
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{prefix}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            file.CopyToAsync(stream).GetAwaiter().GetResult();

        if (!string.IsNullOrEmpty(currentPath))
        {
            var oldPath = Path.Combine(_env.WebRootPath, currentPath.TrimStart('/'));
            if (File.Exists(oldPath)) File.Delete(oldPath);
        }

        return $"/{subDir.Replace("\\", "/")}/{fileName}";
    }

    private static WebsiteSettingDto MapToDto(WebsiteSetting s) => new()
    {
        SiteName = s.SiteName,
        LogoUrl = s.LogoPath,
        FaviconUrl = s.FaviconPath,
        HeroTitle = s.HeroTitle,
        HeroSubtitle = s.HeroSubtitle,
        HeroDescription = s.HeroDescription,
        AboutTitle = s.AboutTitle,
        AboutDescription = s.AboutDescription,
        SupportEmail = s.SupportEmail,
        SupportPhone = s.SupportPhone,
        Address = s.Address,
        FooterText = s.FooterText,
        FooterDescription = s.FooterDescription,
        TwitterUrl = s.TwitterUrl,
        LinkedInUrl = s.LinkedInUrl
    };
}
