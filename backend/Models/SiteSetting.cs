using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoostingHub.backend.Models;

[Table("site_settings")]
public class SiteSetting
{
    [Key] [Column("id")] public int Id { get; set; }

    // ── Branding ────────────────────────────────────────────────────────────
    [Column("site_name")] public string SiteName { get; set; } = "Boosting Hub";
    [Column("logo_path")] public string? LogoPath { get; set; }
    [Column("favicon_path")] public string? FaviconPath { get; set; }

    // ── Hero ────────────────────────────────────────────────────────────────
    [Column("hero_title")] public string? HeroTitle { get; set; }
    [Column("hero_subtitle")] public string? HeroSubtitle { get; set; }
    [Column("hero_description")] public string? HeroDescription { get; set; }

    // ── About ───────────────────────────────────────────────────────────────
    [Column("about_title")] public string? AboutTitle { get; set; }
    [Column("about_description")] public string? AboutDescription { get; set; }

    // ── Contact ─────────────────────────────────────────────────────────────
    [Column("support_email")] public string? SupportEmail { get; set; }
    [Column("support_phone")] public string? SupportPhone { get; set; }
    [Column("address")] public string? Address { get; set; }

    // ── Footer ──────────────────────────────────────────────────────────────
    [Column("footer_text")] public string? FooterText { get; set; }
    [Column("footer_description")] public string? FooterDescription { get; set; }

    // ── Social URLs ─────────────────────────────────────────────────────────
    [Column("twitter_url")] public string? TwitterUrl { get; set; }
    [Column("linkedin_url")] public string? LinkedInUrl { get; set; }

    // ── Metadata ────────────────────────────────────────────────────────────
    [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
