using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Users.Settings;

public class SocialAccountsModel : PageModel
{
    private readonly IAuthenticationService _authService;

    public SocialAccountsModel(IAuthenticationService authService)
    {
        _authService = authService;
    }

    public List<SocialMediaAccountDto> Accounts { get; set; } = new();
    [BindProperty] public List<SocialMediaAccountDto> NewAccounts { get; set; } = new();
    public string? SuccessMessage => TempData["Success"] as string;

    public async Task OnGetAsync()
    {
        await LoadAccountsAsync();
    }

    public async Task<IActionResult> OnPostAddBatchAsync()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Account/Login");

        if (NewAccounts == null || NewAccounts.Count == 0)
        {
            TempData["Error"] = "No accounts provided.";
            return RedirectToPage();
        }

        int added = 0;
        int skipped = 0;

        foreach (var acc in NewAccounts)
        {
            if (string.IsNullOrWhiteSpace(acc.Platform) || string.IsNullOrWhiteSpace(acc.Username))
                continue;

            var result = await _authService.AddSocialMediaAccountAsync(userId.Value, acc);
            if (result.IsSuccess)
                added++;
            else
                skipped++;
        }

        if (added > 0)
            TempData["Success"] = $"Added {added} account(s)." + (skipped > 0 ? $" {skipped} skipped (already linked or invalid)." : "");
        else
            TempData["Error"] = "No accounts were added. They may already be linked.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Account/Login");

        var result = await _authService.DeleteSocialMediaAccountAsync(userId.Value, id);
        if (result.IsSuccess)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;

        return RedirectToPage();
    }

    private async Task LoadAccountsAsync()
    {
        var userId = GetUserId();
        if (userId == null) return;

        var result = await _authService.GetSocialMediaAccountsAsync(userId.Value);
        if (result.IsSuccess && result.Data != null)
            Accounts = result.Data;
    }

    private int? GetUserId()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
            return userId;
        return null;
    }
}
