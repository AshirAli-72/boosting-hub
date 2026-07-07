using BoostingHub.backend.Data;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages.Users.Tasks;

[IgnoreAntiforgeryToken]
public class MyModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ITaskService _taskService;

    public MyModel(ApplicationDbContext db, ITaskService taskService)
    {
        _db = db;
        _taskService = taskService;
    }

    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        UserId = int.TryParse(userIdStr, out var id) ? id : 0;

        if (UserId > 0)
        {
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == UserId);
            UserName = user?.Name ?? "User";
        }
        else
        {
            UserName = "User";
        }
    }

    public async Task<IActionResult> OnPostSubmitProofAsync([FromForm] int taskId, [FromForm] string proofUrl, [FromForm] string proofType)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        var userId = int.TryParse(userIdStr, out var id) ? id : 0;
        if (userId == 0)
            return new JsonResult(new { success = false, message = "Not logged in" });

        if (string.IsNullOrWhiteSpace(proofUrl))
            return new JsonResult(new { success = false, message = "Proof URL is required" });

        var result = await _taskService.SubmitProofAsync(taskId, proofUrl, proofType ?? "URL", userId);
        return new JsonResult(new { success = result.IsSuccess, message = result.Message ?? "Done" });
    }
}
