# Requirements Document

## Introduction

This feature covers a comprehensive UI overhaul and several backend fixes for the **Boosting Hub** ASP.NET Core Razor Pages application. The scope includes:

1. Fixing an empty EF Core migration that left the `campaigns` table missing two columns.
2. Replacing the current dark/gold theme across all pages with a new **deep navy + emerald green + white** SaaS-style premium theme.
3. Redesigning the landing page and adding a "Get Started / Submit Requirements" inquiry form backed by a new `client_inquiries` database table.
4. Redesigning the Login and Register authentication pages with the new theme.
5. Redesigning the User Dashboard with a modern top-navigation layout, gradient stat cards, and updated charts.
6. Auto-redirecting newly registered users directly to the dashboard instead of the login page.

---

## Glossary

- **Application**: The Boosting Hub ASP.NET Core Razor Pages web application.
- **Migration**: An EF Core migration file in `/backend/Data/Migrations/` used to apply or roll back schema changes.
- **Snapshot**: `ApplicationDbContextModelSnapshot.cs`, the EF Core model snapshot file that must stay consistent with applied migrations.
- **Theme**: The visual design system defined in `site.css`, `landing.css`, `auth.css`, and `dashboard.css` under `/wwwroot/css/`.
- **New_Theme**: The replacement design system — deep navy (`#0A1628`) backgrounds, emerald green (`#10B981`) accents, white/light card surfaces, and Inter typeface.
- **Landing_Page**: The publicly accessible root page rendered via `_Layout.cshtml` when `ViewData["PageType"] == "landing"`.
- **Auth_Page**: The Login (`/login`) and Register (`/register`) Razor Pages.
- **Dashboard**: The user dashboard at `/dashboard` rendered by `UsersDashboard.cshtml`.
- **ClientInquiry**: A new EF Core model representing a prospect's service request submitted through the landing page form.
- **InquiryForm**: The "Get Started / Submit Requirements" HTML form on the Landing_Page.
- **Registration_Service**: `AuthenticationService.RegisterAsync` in `/backend/Services/Implementations/AuthenticationService.cs`.
- **Register_Page**: `Register.cshtml` and `Register.cshtml.cs` under `/frontend/Pages/Account/`.
- **Session**: The ASP.NET Core session used to store `UserId` and `AccessToken` after authentication.

---

## Requirements

### Requirement 1: Fix DB Migration – Add Missing Campaign Columns

**User Story:** As a developer, I want the `campaigns` table to have `target_quantity` and `completed_quantity` columns so that the database schema matches the `Campaign` model and the application can run without runtime errors.

#### Acceptance Criteria

1. WHEN the `AlterCampaignsAddNewColumns` migration's `Up()` method runs against a `campaigns` table that does not yet have a `target_quantity` column, THE Migration SHALL execute `migrationBuilder.AddColumn<int>` with column name `"target_quantity"`, table `"campaigns"`, nullable `false`, and default value `0`.
2. WHEN the `AlterCampaignsAddNewColumns` migration's `Up()` method runs against a `campaigns` table that does not yet have a `completed_quantity` column, THE Migration SHALL execute `migrationBuilder.AddColumn<int>` with column name `"completed_quantity"`, table `"campaigns"`, nullable `false`, and default value `0`.
3. WHEN the `AlterCampaignsAddNewColumns` migration's `Down()` method runs, THE Migration SHALL execute `migrationBuilder.DropColumn` for column `"target_quantity"` on table `"campaigns"` only if that column exists.
4. WHEN the `AlterCampaignsAddNewColumns` migration's `Down()` method runs, THE Migration SHALL execute `migrationBuilder.DropColumn` for column `"completed_quantity"` on table `"campaigns"` only if that column exists.
5. WHEN the `AlterCampaignsAddNewColumns` migration has been applied, THE Snapshot `ApplicationDbContextModelSnapshot` SHALL declare `target_quantity` as a non-nullable `int` property with `HasDefaultValue(0)` and `completed_quantity` as a non-nullable `int` property with `HasDefaultValue(0)` on the `Campaign` entity.
6. WHEN the Application starts and `db.Database.Migrate()` runs after the migration is applied, THE Application SHALL start without throwing a `SqlException` referencing `target_quantity` or `completed_quantity`.
7. WHEN `db.Database.Migrate()` encounters an unrecoverable migration error, THE Application SHALL abort startup and log the exception rather than silently continue.

---

### Requirement 2: Apply New Theme to Global Styles

**User Story:** As a visitor or user, I want the entire application to use a consistent, modern premium theme so that the platform feels professional and trustworthy.

#### Acceptance Criteria

1. THE Application SHALL replace all CSS custom property values in `site.css` with the New_Theme colour palette:
   - `--primary: #10B981` (emerald)
   - `--primary-dark: #059669`
   - `--primary-light: #34D399`
   - `--bg-base: #0A1628` (deep navy)
   - `--bg-surface: #0F1F3D`
   - `--bg-card: #162040`
   - `--bg-elevated: #1A2B50`
   - `--text-primary: #FFFFFF`
   - `--text-secondary: rgba(255,255,255,0.7)`
   - `--text-muted: rgba(255,255,255,0.45)`
   - `--primary-glow: rgba(16,185,129,0.25)`
   - `--shadow-glow: 0 8px 32px rgba(16,185,129,0.2)`
   - `--sidebar-active: rgba(16,185,129,0.12)`
2. WHEN `body.auth-page-body` is rendered, THE body background SHALL be a radial-gradient using `radial-gradient(circle at 20% 50%, rgba(16,185,129,0.12) 0%, transparent 60%), radial-gradient(circle at 80% 50%, rgba(16,185,129,0.08) 0%, transparent 60%), #0A1628` so the auth pages have a navy base with subtle emerald glows.
3. WHEN `body.landing-page` is rendered, THE body background SHALL use `#0A1628` as the base background colour so the landing page defaults to the New_Theme deep navy.
4. WHEN light-mode is active (`.light-mode` class on `html`), THE `site.css` light-mode overrides SHALL set `--bg-base: #F8FAFC`, `--bg-surface: #FFFFFF`, `--bg-card: #FFFFFF`, `--text-primary: #1A202C`, `--text-secondary: #4A5568`, and `--primary: #059669` so the emerald accent remains consistent in light mode.
5. THE `site.css` file SHALL retain the Inter font `@import` from Google Fonts and all non-colour utility variables (border radius, transition timing, spacing tokens) unchanged while replacing only colour values per criteria 1–4.

---

### Requirement 3: Redesign Landing Page with New Theme

**User Story:** As a visitor, I want to see a visually compelling landing page using the new navy and emerald theme so that I feel confident about the platform's credibility.

#### Acceptance Criteria

1. THE `landing.css` file SHALL replace every occurrence of `#D09010`, `#B07D10`, and `#F0B030` with the corresponding New_Theme emerald value: `#D09010` → `#10B981`, `#B07D10` → `#059669`, `#F0B030` → `#34D399`, with no gold values remaining in the file.
2. THE landing header in `_Layout.cshtml` SHALL use `background: #0A1628` and the primary CTA button (`.btn-primary-solid`) SHALL use `background: linear-gradient(135deg, #10B981 0%, #059669 100%)` with a hover box-shadow of `0 8px 28px rgba(16,185,129,0.45)`.
3. THE hero section SHALL update the badge background to `rgba(16,185,129,0.12)` with border `rgba(16,185,129,0.25)` and text `#34D399`; the title gradient SHALL use `linear-gradient(135deg, #34D399 0%, #10B981 50%, #059669 100%)`; hero background glows SHALL use `rgba(16,185,129,0.15)` and `rgba(16,185,129,0.12)` replacing gold equivalents; hero card icons SHALL use emerald tints matching the icon colour scheme.
4. THE features section icon backgrounds SHALL use: `.feature-icon.purple { background: rgba(16,185,129,0.15); color: #10B981 }` and all other feature icon variants SHALL use their existing green/blue/orange/teal colours unchanged — only the gold-mapped "purple" icon SHALL change.
5. THE stats section (`.section-light`) SHALL use `background: #F8FAFC` with stat numbers styled as `background: linear-gradient(135deg, #10B981 0%, #059669 100%); -webkit-background-clip: text; -webkit-text-fill-color: transparent` replacing the gold gradient.
6. THE CTA section (`.cta-section`) SHALL use `background: linear-gradient(135deg, #10B981 0%, #059669 100%)` and the `.btn-cta` text colour SHALL be `#059669` (not gold) so the button remains legible on the emerald background.
7. THE landing footer (`.landing-footer`) SHALL use `background: #071020` and all footer link hover colours SHALL be `#10B981`; footer social icon hover background SHALL be `rgba(16,185,129,0.15)` with icon colour `#10B981`.

---

### Requirement 4: Add Client Inquiry Form to Landing Page

**User Story:** As a prospective client, I want to submit my service requirements directly from the landing page so that I can quickly express my needs without registering first.

#### Acceptance Criteria

1. THE `_Layout.cshtml` landing section SHALL include a new `InquiryForm` section inserted immediately after the stats section (`#stats`) and before the CTA section, with `id="get-started"`.
2. THE InquiryForm SHALL contain the following fields:
   - Platform (required `<select>`: Instagram, YouTube, TikTok, Twitter, Facebook)
   - Service Type (required `<select>`: Followers, Likes, Views, Comments)
   - Quantity (required `<input type="number">`, minimum 100, maximum 1,000,000)
   - Target URL (required `<input type="url">`, must be a valid URL format)
   - Budget (required `<input type="number">`, minimum 0.01, maximum 999,999.99, step 0.01)
   - Notes (optional `<textarea>`, maximum 1000 characters)
3. WHEN a visitor submits the InquiryForm and all required fields pass their validation rules (Platform selected, Service Type selected, Quantity between 100–1,000,000, Target URL is a valid URL, Budget between 0.01–999,999.99), THE Application SHALL persist a new `ClientInquiry` record to the `client_inquiries` table with all submitted values and `CreatedAt` set to `DateTime.UtcNow`.
4. IF a visitor submits the InquiryForm and any required field fails validation, THEN THE Application SHALL redisplay the InquiryForm section with inline field-level validation error messages adjacent to each invalid field, and all previously entered field values SHALL be preserved.
5. WHEN a `ClientInquiry` is successfully saved, THE Application SHALL display a success message immediately above the InquiryForm (e.g., "Your request has been submitted! We'll get back to you shortly.") styled with the New_Theme emerald success style, and the form fields SHALL be cleared.
6. THE `ClientInquiry` model SHALL be defined in `/backend/Models/ClientInquiry.cs` with properties: `Id` (int, PK, identity), `Platform` (string, max 50, required), `ServiceType` (string, max 50, required), `Quantity` (int, required), `TargetUrl` (string, max 500, required), `Budget` (decimal(18,2), required), `Notes` (string, max 1000, nullable), `CreatedAt` (DateTime, required).
7. THE `ApplicationDbContext` SHALL include a `DbSet<ClientInquiry> ClientInquiries` property and a fluent configuration in `OnModelCreating` that sets max lengths matching the model and maps to the table name `"client_inquiries"`.
8. THE Application SHALL include a new EF Core migration file named `AddClientInquiriesTable` that creates the `client_inquiries` table with columns and constraints matching the `ClientInquiry` model definition.
9. THE `ApplicationDbContextModelSnapshot` SHALL be updated to include the `ClientInquiry` entity with all properties, constraints, and table mapping after the `AddClientInquiriesTable` migration is applied.

---

### Requirement 5: Redesign Authentication Pages (Login & Register)

**User Story:** As a user, I want the login and register pages to look modern and premium with the new theme so that the authentication experience feels polished and trustworthy.

#### Acceptance Criteria

1. THE `auth.css` file SHALL replace all indigo/gold colour literals — `#6366f1`, `#818cf8`, `#4f46e5`, `#B07D10`, `rgba(99,102,241,...)` — with New_Theme emerald equivalents: `#6366f1` → `#10B981`, `#818cf8` → `#34D399`, `#4f46e5` → `#059669`, `#B07D10` → `#059669`, and `rgba(99,102,241,X)` → `rgba(16,185,129,X)` preserving the original alpha value.
2. THE `Login.cshtml` page SHALL replace the current `.auth-container` two-column split-panel layout with a single centered `.auth-card` element on a full-page background; the page background SHALL be `#0A1628` with `radial-gradient(circle at 30% 40%, rgba(16,185,129,0.10) 0%, transparent 55%)` overlay.
3. THE `Register.cshtml` page SHALL use the identical centered single-card layout as the redesigned Login page, with the same background treatment.
4. THE `.auth-card` on both pages SHALL have `background: #FFFFFF`, `border-radius: 24px`, `border-top: 4px solid #10B981`, and `box-shadow: 0 20px 60px rgba(0,0,0,0.25)`.
5. WHEN an input field on either auth page receives focus, THE field border SHALL change to `2px solid #10B981` and a focus ring of `box-shadow: 0 0 0 4px rgba(16,185,129,0.15)` SHALL appear; unfocused fields SHALL use `border: 2px solid #e2e8f0`.
6. THE submit button on both auth pages SHALL use `background: linear-gradient(135deg, #10B981 0%, #059669 100%)` and on `:hover` SHALL display `box-shadow: 0 8px 25px rgba(16,185,129,0.35)` with `transform: translateY(-2px)`, replacing all indigo-gold hover shadows.
7. THE `Login.cshtml` and `Register.cshtml` pages SHALL retain all existing `<form>`, `asp-for`, `asp-validation-for`, `asp-validation-summary`, `@if (!string.IsNullOrEmpty(Model.ErrorMessage))` blocks, and footer navigation links — only CSS class names and layout wrapper elements change.

---

### Requirement 6: Redesign User Dashboard

**User Story:** As a logged-in user, I want a modern dashboard with a top navigation bar and gradient stat cards so that my workspace feels spacious and visually engaging.

#### Acceptance Criteria

1. THE `UsersDashboard.cshtml` page SHALL remove the `<aside class="admin-sidebar user-sidebar">` element entirely and replace it with a `<nav class="dash-topnav">` element containing: the Boosting Hub logo image, navigation links (Dashboard `/dashboard`, Available Tasks `/tasks`, Security `/security`), a theme toggle button (`id="themeToggle"`), and the user badge showing `@Model.Dashboard.UserName`.
2. THE four stat cards in `UsersDashboard.cshtml` SHALL each use `background: linear-gradient(135deg, <start-colour> 0%, <end-colour> 100%)` gradients:
   - Total Tasks: `#10B981` → `#059669`
   - Completed: `#3B82F6` → `#1D4ED8`
   - Pending: `#F59E0B` → `#D97706`
   - Rewards: `#8B5CF6` → `#6D28D9`
   Each card SHALL display the icon, label, and value in white text on the gradient background.
3. THE `dashboard.css` file SHALL replace every `#6366f1` literal with `#10B981` and every `rgba(99,102,241,X)` with `rgba(16,185,129,X)` (preserving alpha), while leaving all layout properties (display, flex, grid, padding, margin, width, height) unchanged.
4. WHEN the viewport width is below 768px, THE `dash-topnav` navigation links SHALL be hidden and a hamburger button SHALL be visible; WHEN the user clicks the hamburger, THE navigation links SHALL toggle to a visible dropdown state; WHEN the user clicks the hamburger again, THE dropdown SHALL close.
5. THE line chart datasets block in `UsersDashboard.cshtml` `<script>` section SHALL set `borderColor: '#10B981'` and `backgroundColor: 'rgba(16,185,129,0.1)'` replacing the current indigo values.
6. THE `dashboard.css` file SHALL retain all existing structural selectors (`.admin-layout`, `.admin-main`, `.admin-content`, `.admin-header`, `.stat-card`, `.chart-card`, `.sidebar-brand`, `.sidebar-nav`, `.sidebar-link`) and only change colour-related properties within those selectors per criteria 2 and 3.

---

### Requirement 8: Admin Credentials Must Route to Admin Panel

**User Story:** As an admin user, when I log in with admin credentials I want to be routed to the admin panel, not the user dashboard, so that I have access to admin-only features.

#### Acceptance Criteria

1. WHEN `Login.cshtml.cs` `OnPostAsync` receives a successful login result and the user's `Roles` array contains any value that includes the substring `"Admin"` (case-insensitive), THE handler SHALL redirect to `/admin-panel` (`RedirectToPage("/Admin/AdminPanel")`).
2. WHEN the user's `Roles` array contains no value matching `"Admin"`, THE handler SHALL redirect to `/dashboard` (`RedirectToPage("/User/UsersDashboard")`).
3. THE admin redirect logic SHALL remain consistent with the existing `roles.Any(r => r.Contains("Admin"))` check already present in `Login.cshtml.cs` — this requirement documents and locks that behaviour.
4. WHEN an admin user visits `/dashboard` directly (not via login), THE page SHALL still load without error (no role-based redirect guard is required on the dashboard page itself).

---

### Requirement 9: Security Page Must Not Hide Available Tasks Link in Sidebar

**User Story:** As a user on the Security Settings page, I want the "Available Tasks" sidebar link to remain visible so that I can navigate to tasks without going back to the dashboard first.

#### Acceptance Criteria

1. THE `SecuritySettings.cshtml` sidebar `<nav>` SHALL include an `<a href="/tasks" class="sidebar-link">` link with icon `bi-list-task` and label "Available Tasks", positioned between the Dashboard link and the Security link.
2. WHEN the Security Settings page is active, THE sidebar SHALL show three navigation links: Dashboard, Available Tasks, and Security — matching the sidebar structure on `UsersDashboard.cshtml` and `Tasks/Index.cshtml`.
3. THE Security link SHALL retain the `active` class on the Security Settings page to indicate the current page.
4. THE Available Tasks link on the Security Settings page SHALL NOT have the `active` class when the Security Settings page is active.

---

### Requirement 7: Auto-Redirect After Registration

**User Story:** As a new user, I want to be redirected directly to my dashboard after registering so that I can start using the platform immediately without needing to log in again.

#### Acceptance Criteria

1. WHEN `Registration_Service.RegisterAsync` returns a success result, THE `Register.cshtml.cs` `OnPostAsync` handler SHALL call `HttpContext.Session.SetString("AccessToken", result.Data!.AccessToken ?? "")` before performing any redirect.
2. WHEN `Registration_Service.RegisterAsync` returns a success result and `result.Data.User` is non-null, THE `Register.cshtml.cs` `OnPostAsync` handler SHALL call `HttpContext.Session.SetString("UserId", result.Data.User.Id.ToString())` before performing any redirect.
3. WHEN session values have been stored per criteria 1 and 2, THE `Register.cshtml.cs` `OnPostAsync` handler SHALL return `RedirectToPage("/User/UsersDashboard")` so the user lands on their dashboard immediately after registration.
4. IF `Registration_Service.RegisterAsync` returns a failure result, THEN `OnPostAsync` SHALL assign `result.Message` to `Model.ErrorMessage` and `result.Errors` to `Model.Errors`, return `Page()`, and SHALL NOT store any session values or redirect.
5. IF `ModelState.IsValid` is `false` when `OnPostAsync` is invoked, THEN the handler SHALL return `Page()` immediately without calling `RegisterAsync`, without setting session values, and without redirecting.
6. IF `Registration_Service.RegisterAsync` returns a success result but `result.Data` is null or `result.Data.AccessToken` is null or `result.Data.User` is null, THEN `OnPostAsync` SHALL treat the response as a failure, set `ErrorMessage` to a safe fallback message, and return `Page()` without redirecting.
7. THE `Registration_Service.RegisterAsync` method SHALL populate `authResponse.User` with `Id`, `Name`, `Email`, `Phone`, `Status`, and `Roles` on a successful registration (mirroring the login path) so that `result.Data.User.Id` is available to `Register.cshtml.cs`.
