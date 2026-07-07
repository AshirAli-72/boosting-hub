using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Admin.Settings;

public class UsersModel : PageModel
{
    private readonly IUserManagementService _userService;
    private readonly IRoleService _roleService;

    public UsersModel(IUserManagementService userService, IRoleService roleService)
    {
        _userService = userService;
        _roleService = roleService;
    }

    public List<UserWithRolesDto> Users { get; set; } = new();
    public List<RoleBasicDto> AllRoles { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public int EditUserId { get; set; }
    public bool IsEditing { get; set; }

    [BindProperty] public CreateUserDto CreateInput { get; set; } = new();
    [BindProperty] public List<int> EditRoleIds { get; set; } = new();

    public async Task OnGetAsync(int? edit)
    {
        await LoadDataAsync();
        if (edit.HasValue)
        {
            IsEditing = true;
            EditUserId = edit.Value;
            var user = Users.FirstOrDefault(u => u.Id == edit.Value);
            if (user != null)
                EditRoleIds = user.Roles.Select(r => r.Id).ToList();
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var result = await _userService.CreateUserAsync(CreateInput);
        if (result.IsSuccess)
        {
            SuccessMessage = result.Message;
            return RedirectToPage();
        }
        ErrorMessage = result.Message;
        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateRolesAsync(int userId)
    {
        var result = await _userService.UpdateUserRolesAsync(userId, EditRoleIds);
        if (result.IsSuccess)
        {
            SuccessMessage = result.Message;
            return RedirectToPage();
        }
        ErrorMessage = result.Message;
        await LoadDataAsync();
        return Page();
    }

    private async Task LoadDataAsync()
    {
        var usersResult = await _userService.GetUsersAsync();
        if (usersResult.IsSuccess)
            Users = usersResult.Data!;

        var rolesResult = await _roleService.GetRolesAsync();
        if (rolesResult.IsSuccess)
            AllRoles = rolesResult.Data!.Select(r => new RoleBasicDto { Id = r.Id, RoleTitle = r.RoleTitle }).ToList();
    }
}
