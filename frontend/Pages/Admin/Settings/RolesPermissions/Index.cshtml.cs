using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Admin.Settings.RolesPermissions;

public class IndexModel : PageModel
{
    private readonly IRoleService _roleService;

    public IndexModel(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public List<RoleWithPermissionsDto> Roles { get; set; } = new();
    public List<PermissionDto> AllPermissions { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty] public CreateRoleDto CreateInput { get; set; } = new();
    [BindProperty] public UpdateRoleDto EditInput { get; set; } = new();
    public int EditRoleId { get; set; }
    public bool IsEditing { get; set; }

    public async Task OnGetAsync(int? edit)
    {
        await LoadDataAsync();
        if (edit.HasValue)
        {
            IsEditing = true;
            EditRoleId = edit.Value;
            var role = Roles.FirstOrDefault(r => r.Id == edit.Value);
            if (role != null)
            {
                EditInput.RoleTitle = role.RoleTitle;
                EditInput.Description = role.Description;
                EditInput.PermissionIds = role.Permissions.Select(p => p.Id).ToList();
            }
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var result = await _roleService.CreateRoleAsync(CreateInput);
        if (result.IsSuccess)
        {
            SuccessMessage = result.Message;
            return RedirectToPage();
        }
        ErrorMessage = result.Message;
        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int roleId)
    {
        var result = await _roleService.UpdateRoleAsync(roleId, EditInput);
        if (result.IsSuccess)
        {
            SuccessMessage = result.Message;
            return RedirectToPage();
        }
        ErrorMessage = result.Message;
        await LoadDataAsync();
        IsEditing = true;
        EditRoleId = roleId;
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int roleId)
    {
        var result = await _roleService.DeleteRoleAsync(roleId);
        if (result.IsSuccess)
            SuccessMessage = result.Message;
        else
            ErrorMessage = result.Message;
        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        var rolesResult = await _roleService.GetRolesAsync();
        if (rolesResult.IsSuccess)
            Roles = rolesResult.Data!;

        var permsResult = await _roleService.GetPermissionsAsync();
        if (permsResult.IsSuccess)
            AllPermissions = permsResult.Data!;
    }
}
