using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace BoostingHub.backend.Services.Implementations;

public class SiteSettingService : ISiteSettingService
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    private const string LogoDir = "uploads\\site\\logo";
    private const string FaviconDir = "uploads\\site\\favicon";

    public SiteSettingService(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<Result<SiteSettingDto>> GetAsync()
    {
        var setting = await _db.SiteSettings.AsNoTracking().FirstOrDefaultAsync();
        if (setting == null)
        {
            setting = new SiteSetting();
            _db.SiteSettings.Add(setting);
            await _db.SaveChangesAsync();
        }

        return Result<SiteSettingDto>.Success(MapToDto(setting));
    }

    public async Task<Result> UpdateAsync(SiteSettingDto dto)
    {
        var setting = await _db.SiteSettings.FirstOrDefaultAsync();
        if (setting == null)
        {
            setting = new SiteSetting();
            _db.SiteSettings.Add(setting);
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

    private static SiteSettingDto MapToDto(SiteSetting s) => new()
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
