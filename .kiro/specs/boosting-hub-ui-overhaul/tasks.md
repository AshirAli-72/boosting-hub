# Implementation Plan: Boosting Hub UI Overhaul

## Overview

This plan covers all 18 coding tasks across 5 groups. Groups 1–2 are pure backend/service work with no UI dependencies. Group 3 (CSS) can be done in parallel across all four stylesheets. Group 4 (HTML/Razor) depends on the CSS tasks. Group 5 (inquiry form) depends on the DB/model tasks from Group 1.

Language: **C# / ASP.NET Core Razor Pages** (existing stack, no new frameworks).

---

## Tasks

### Group 1 – Backend / DB Fixes

- [ ] 1. Fix migration and DB schema for Campaign columns
  - [ ] 1.1 Fill in `AlterCampaignsAddNewColumns` migration `Up()` and `Down()` methods
    - File: `backend/Data/Migrations/20260701133926_AlterCampaignsAddNewColumns.cs`
    - `Up()`: call `migrationBuilder.AddColumn<int>` twice — `name: "target_quantity"` and `name: "completed_quantity"`, both on table `"campaigns"`, `nullable: false`, `defaultValue: 0`
    - `Down()`: call `migrationBuilder.DropColumn` for each column name on `"campaigns"`
    - The snapshot already reflects both properties — no snapshot changes needed (design §4.3)
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [ ] 1.2 Create `ClientInquiry` model
    - New file: `backend/Models/ClientInquiry.cs`
    - Add `[Table("client_inquiries")]` attribute; properties: `Id` (int PK identity), `Platform` (string max 50, required), `ServiceType` (string max 50, required), `Quantity` (int required), `TargetUrl` (string max 500, required), `Budget` (decimal required), `Notes` (string max 1000, nullable), `CreatedAt` (DateTime, default `DateTime.UtcNow`)
    - Use `[Column("snake_case_name")]` attributes matching the design (design §5.1)
    - _Requirements: 4.6_

  - [ ] 1.3 Register `ClientInquiry` in `ApplicationDbContext`
    - File: `backend/Data/ApplicationDbContext.cs`
    - Add `public DbSet<ClientInquiry> ClientInquiries { get; set; }` alongside existing DbSets
    - Add fluent config block in `OnModelCreating`: `e.ToTable("client_inquiries")`, max-length constraints for Platform, ServiceType, TargetUrl, Notes; `decimal(18,2)` for Budget (design §5.2)
    - _Requirements: 4.7_

  - [ ] 1.4 Hand-write `AddClientInquiriesTable` migration file
    - New file: `backend/Data/Migrations/20260702000000_AddClientInquiriesTable.cs`
    - Class must be `public partial class AddClientInquiriesTable : Migration`
    - `Up()`: `migrationBuilder.CreateTable("client_inquiries", ...)` with all columns and `PrimaryKey("PK_client_inquiries", x => x.id)`
    - `Down()`: `migrationBuilder.DropTable("client_inquiries")`
    - Follow the exact column types from design §5.3 (id identity, strings with maxLength, decimal(18,2), nullable notes, datetime created_at)
    - Do NOT run `dotnet ef migrations add` — write the file manually
    - _Requirements: 4.8_

  - [ ] 1.5 Update `ApplicationDbContextModelSnapshot` to include `ClientInquiry`
    - File: `backend/Data/Migrations/ApplicationDbContextModelSnapshot.cs`
    - Add the `modelBuilder.Entity("BoostingHub.backend.Models.ClientInquiry", b => { ... })` block with all seven properties, their column types, maxLengths, nullability, and `b.ToTable("client_inquiries")`
    - Add `b.HasKey("Id")` — no extra indexes needed
    - Mirror the existing entity pattern used for `Notification` or `Transaction` as a template
    - _Requirements: 4.9_

---

### Group 2 – Auth Service Fix

- [ ] 2. Fix registration flow to populate user DTO and auto-redirect
  - [ ] 2.1 Update `AuthenticationService.RegisterAsync` to populate `authResponse.User`
    - File: `backend/Services/Implementations/AuthenticationService.cs`
    - After `var authResponse = await _tokenService.GenerateTokensAsync(user);`, add a query to load the user with roles (`.Include(u => u.UserHasRoles).ThenInclude(ur => ur.Role)`)
    - Assign `authResponse.User = new UserDto { Id, Name, Email, Phone, Status, EmailVerifiedAt, Roles }` — mirroring the `LoginAsync` pattern exactly (design §8.1)
    - The guest role was just assigned so `UserHasRoles` will have one entry
    - _Requirements: 7.7_

  - [ ] 2.2 Update `Register.cshtml.cs` `OnPostAsync` to store session and redirect to dashboard
    - File: `frontend/Pages/Account/Register.cshtml.cs`
    - Replace the current `return RedirectToPage("/Account/Login")` success branch
    - Add null guard: if `result.Data == null || result.Data.User == null`, set `ErrorMessage` to a safe fallback and return `Page()` — do not redirect
    - On clean success: `HttpContext.Session.SetString("AccessToken", result.Data.AccessToken ?? "")`, `HttpContext.Session.SetString("UserId", result.Data.User.Id.ToString())`, then `return RedirectToPage("/User/UsersDashboard")` (design §8.2)
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_

---

### Group 3 – CSS Theme Overhaul (all four tasks can be done in parallel)

- [ ] 3. Replace colour tokens and theme across all stylesheets
  - [ ] 3.1 Rewrite `site.css` with new deep navy + emerald token map
    - File: `wwwroot/css/site.css`
    - Replace the `:root` block entirely with the new token map from design §3.1: `--primary: #10B981`, `--primary-dark: #059669`, `--primary-light: #34D399`, `--primary-glow: rgba(16,185,129,0.25)`, `--shadow-glow: 0 8px 32px rgba(16,185,129,0.2)`, surface variables `--bg-base: #0A1628`, `--bg-surface: #0F1F3D`, `--bg-card: #162040`, `--bg-elevated: #1A2B50`, text and sidebar tokens
    - Update `body.auth-page-body` background to use emerald radial gradients (design §3.2)
    - Update `body.light-mode` overrides (design §3.3)
    - Keep the Inter `@import`, all non-colour layout/utility variables, and all body class rules unchanged
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [ ] 3.2 Rewrite `auth.css` replacing all indigo/gold with emerald equivalents
    - File: `wwwroot/css/auth.css`
    - Apply the replacement map from design §3.5: `#6366f1` → `#10B981`, `#818cf8` → `#34D399`, `#4f46e5` → `#059669`, `#B07D10` → `#059669`, `rgba(99,102,241,X)` → `rgba(16,185,129,X)` (preserve alpha)
    - Update `.auth-card` rule to add `border-top: 4px solid #10B981` and white background (design §6.2)
    - Update `.btn-auth` gradient to `linear-gradient(135deg, #10B981 0%, #059669 100%)` and hover shadow to `rgba(16,185,129,0.35)` (design §6.4)
    - Update input focus states: `border-color: #10B981`, `box-shadow: 0 0 0 4px rgba(16,185,129,0.15)` (design §6.3)
    - All structural rules (display, flex, padding, animation, responsive breakpoints) stay unchanged
    - _Requirements: 5.1, 5.4, 5.5, 5.6_

  - [ ] 3.3 Rewrite `landing.css` replacing all gold with emerald equivalents
    - File: `wwwroot/css/landing.css`
    - Apply the replacement map from design §3.4: `#D09010` → `#10B981`, `#B07D10` → `#059669`, `#F0B030` → `#34D399`, `rgba(208,144,16,X)` → `rgba(16,185,129,X)` (preserve alpha)
    - This covers: `.header-logo-icon` background, `.btn-primary-solid` gradient and shadow, `.hero-bg-glow`/`hero-bg-glow-2`, `.hero-badge`, `.hero-title-gradient`, `.hero-card-icon.purple`, `.hero-card-avatar-circle`, `.feature-icon.purple`, `.section-label`, `.stat-number`, `.btn-hero-primary`, `.cta-section` gradient, `.btn-cta` text colour, `.footer-col a:hover`, `.footer-social a:hover`
    - Zero gold hex values should remain in the file after this change
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_

  - [ ] 3.4 Update `dashboard.css` — replace indigo with emerald, add dash-topnav styles, fix admin-main margin
    - File: `wwwroot/css/dashboard.css`
    - Replace every `#6366f1` with `#10B981` and every `rgba(99,102,241,X)` with `rgba(16,185,129,X)` (design §3.6)
    - Set `.admin-main { margin-left: 0; }` (replaces `margin-left: 256px`) since the user dashboard removes the sidebar (design §7.1)
    - Add all `.dash-topnav`, `.topnav-brand`, `.topnav-links`, `.topnav-link`, `.topnav-right`, `.topnav-hamburger` rules verbatim from design §7.4
    - Add `.stat-card-gradient` white colour override rule from design §7.2
    - All existing structural selectors (`.admin-layout`, `.admin-sidebar`, `.sidebar-nav`, `.stat-card`, `.chart-card`, etc.) are retained; only colour values change
    - _Requirements: 6.3, 6.4, 6.6_

---

### Group 4 – Page / HTML Changes (depends on Group 3 CSS tasks)

- [ ] 4. Redesign and fix Razor Pages
  - [ ] 4.1 Redesign `Login.cshtml` — replace split-panel with single auth-card layout
    - File: `frontend/Pages/Account/Login.cshtml`
    - Remove outer `<div class="auth-container">`, `<div class="auth-brand">` + content, and `<div class="auth-form">` wrappers
    - Replace with single `<div class="auth-card">` directly inside `<div class="auth-page">`
    - Move logo `<img class="auth-form-logo">`, `<h3 class="auth-form-title">`, subtitle `<p>`, and the entire `<form>` block (with all `asp-for`, `asp-validation-for`, error block, submit button, divider, and footer link) inside `.auth-card`
    - The `<div class="auth-page">` stays; only the inner wrapper changes (design §6.1)
    - _Requirements: 5.2, 5.7_

  - [ ] 4.2 Redesign `Register.cshtml` — replace split-panel with single auth-card layout
    - File: `frontend/Pages/Account/Register.cshtml`
    - Same structural change as T12 (4.1): remove `.auth-container`, `.auth-brand`, `.auth-form` wrappers; wrap all content in `.auth-card`
    - Preserve all five `asp-for` input fields (Name, Email, Phone, Password, ConfirmPassword), error display blocks (`Model.ErrorMessage`, `Model.Errors`), validation summary div, submit button, divider, and "already have an account" footer link — nothing removed except the split-panel wrapper divs (design §6.1, requirements §5.3, §5.7)
    - _Requirements: 5.3, 5.7_

  - [ ] 4.3 Add "Available Tasks" sidebar link to `SecuritySettings.cshtml`
    - File: `frontend/Pages/Account/SecuritySettings.cshtml`
    - In the `<nav class="sidebar-nav">`, insert `<a href="/tasks" class="sidebar-link"><i class="bi bi-list-task"></i> Available Tasks</a>` between the Dashboard link and the Security link
    - Security link retains `class="sidebar-link active"` — no active class on the new link
    - Final order: Dashboard → Available Tasks → Security → (divider) → Logout (design §10.1)
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

  - [ ] 4.4 Redesign `UsersDashboard.cshtml` — topnav, gradient stat cards, emerald chart colours
    - File: `frontend/Pages/User/UsersDashboard.cshtml`
    - Remove the entire `<aside class="admin-sidebar user-sidebar">` block
    - Remove the `<div class="admin-header">` block (the topnav replaces it)
    - Add `<nav class="dash-topnav">` as the first child of `.admin-layout`, containing: brand logo link, `.topnav-links` with Dashboard/Available Tasks/Security links (Dashboard gets `active` class), `.topnav-right` with themeToggle button, user badge, logout link, and hamburger button — exact HTML from design §7.1
    - Update each of the four `<div class="stat-card">` elements: add class `stat-card-gradient`, add inline `style="background: linear-gradient(135deg, ...)"` per card (Total Tasks: `#10B981→#059669`, Completed: `#3B82F6→#1D4ED8`, Pending: `#F59E0B→#D97706`, Rewards: `#8B5CF6→#6D28D9`), remove `stat-icon-*` classes from icons (design §7.2)
    - In the `<script>` section, update the line chart dataset: `borderColor: '#10B981'` and `backgroundColor: 'rgba(16,185,129,0.1)'` (design §7.3)
    - Add a small inline JS snippet for the hamburger toggle (`topnavHamburger` click → `.topnav-links.toggle('open')`)
    - _Requirements: 6.1, 6.2, 6.4, 6.5_

---

### Group 5 – Landing Page Inquiry Form (depends on Tasks 1.2, 1.3, 1.4, 1.5)

- [ ] 5. Build client inquiry form end-to-end
  - [ ] 5.1 Create `Index.cshtml.cs` (IndexModel) with InquiryInputModel, OnGet, OnPostAsync
    - New file: `frontend/Pages/Index.cshtml.cs`
    - Inject `ApplicationDbContext` via constructor
    - Define `InquiryInputModel` as a nested/sibling class with: Platform, ServiceType, Quantity, TargetUrl, Budget (all required with the validation attributes from design §5.4), Notes (optional, max 1000)
    - `[BindProperty] public InquiryInputModel Inquiry` and `public bool ShowSuccess`
    - `OnGet()`: no-op
    - `OnPostAsync()`: validate ModelState → map to `ClientInquiry` entity → `_db.ClientInquiries.Add(entity)` → `await _db.SaveChangesAsync()` → set `ShowSuccess = true`, clear `ModelState`, reset `Inquiry = new InquiryInputModel()`, return `Page()`; on invalid: return `Page()` (design §5.4)
    - _Requirements: 4.3, 4.4, 4.5_

  - [ ] 5.2 Update `Index.cshtml` to add the `#get-started` inquiry form section
    - File: `frontend/Pages/Index.cshtml` (create if not present, or update the existing landing page body file)
    - Add `@page "/"` and `@model BoostingHub.frontend.Pages.IndexModel` at the top
    - The page body should render all the landing page content (hero, features, stats) via the layout, but **the inquiry form section lives here** as `@RenderBody()` output
    - Insert the `<section class="inquiry-section" id="get-started">` block after the stats content; include: section header, success message conditional (`@if (Model.ShowSuccess)`), and the `<form method="post">` with `@Html.AntiForgeryToken()`, six field groups using `asp-for="Inquiry.Platform"` etc., `<span asp-validation-for>` for each field, and the submit button (design §5.5)
    - Preserve `ViewData["PageType"] = "landing"` and `ViewData["BodyClass"] = "landing-page"` so the layout renders the full landing shell
    - _Requirements: 4.1, 4.2, 4.4, 4.5_

  - [ ] 5.3 Update `_Layout.cshtml` landing nav header to include `#get-started` link
    - File: `frontend/Pages/Shared/_Layout.cshtml`
    - In `<nav class="header-nav" id="headerNav">`, add `<a href="#get-started">Get Started</a>` as the last nav item (after the `#stats` link)
    - _Requirements: 4.1_

- [ ] 6. Final checkpoint — ensure all tests pass
  - Build the project (`dotnet build`) — confirm zero compile errors
  - Verify `db.Database.Migrate()` on startup applies both migrations without `SqlException`
  - Confirm the landing page loads at `/`, the inquiry form submits and shows a success message, login/register pages render as single-card layouts, the user dashboard shows the topnav, and the security settings sidebar shows all three navigation links
  - Ask the user if any questions arise before closing

---

## Notes

- Tasks marked with `*` are optional sub-tasks (none in this plan — no property-based tests apply here as the design has no Correctness Properties section)
- Groups 3.1–3.4 can be executed in parallel (no shared file writes within the group)
- Group 4.1 and 4.2 both depend on Group 3.2 (`auth.css`) being done first
- Group 4.3 and 4.4 both depend on Group 3.4 (`dashboard.css`) being done first
- Group 5 tasks depend on Tasks 1.2, 1.3, 1.4, and 1.5 all being complete
- The snapshot (Task 1.5) must be updated manually since the migration file is hand-written, not generated by `dotnet ef`
- `Login.cshtml.cs` is intentionally not modified (Req 8 locks its routing logic — design §9)
- The `.admin-main { margin-left: 0 }` change in `dashboard.css` (Task 3.4) only affects `UsersDashboard.cshtml` layout; `Tasks/Index.cshtml` and `SecuritySettings.cshtml` still use the sidebar layout, so their `admin-main` margin is overridden back via the sidebar CSS that remains intact

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "3.1", "3.2", "3.3", "3.4"] },
    { "id": 1, "tasks": ["1.3", "2.1"] },
    { "id": 2, "tasks": ["1.4", "2.2", "4.1", "4.2", "4.3", "4.4"] },
    { "id": 3, "tasks": ["1.5"] },
    { "id": 4, "tasks": ["5.1"] },
    { "id": 5, "tasks": ["5.2", "5.3"] }
  ]
}
```
