using Microsoft.AspNetCore.Http;

namespace BoostingHub.backend.DTOs;

public class WebsiteSettingDto
{
    // Branding
    public string SiteName { get; set; } = "Boosting Hub";
    public string? LogoUrl { get; set; }
    public IFormFile? LogoFile { get; set; }
    public string? FaviconUrl { get; set; }
    public IFormFile? FaviconFile { get; set; }

    // Hero
    public string? HeroTitle { get; set; }
    public string? HeroSubtitle { get; set; }
    public string? HeroDescription { get; set; }

    // About
    public string? AboutTitle { get; set; }
    public string? AboutDescription { get; set; }

    // Contact
    public string? SupportEmail { get; set; }
    public string? SupportPhone { get; set; }
    public string? Address { get; set; }

    // Footer
    public string? FooterText { get; set; }
    public string? FooterDescription { get; set; }

    // Social
    public string? TwitterUrl { get; set; }
    public string? LinkedInUrl { get; set; }
}
