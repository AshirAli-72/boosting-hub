using BoostingHub.backend.Data;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.frontend.Pages.Users.Tasks;

[IgnoreAntiforgeryToken]
[DisableRequestSizeLimit]
[RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
public class MyModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ITaskService _taskService;
    private readonly IWebHostEnvironment _env;

    public MyModel(ApplicationDbContext db, ITaskService taskService, IWebHostEnvironment env)
    {
        _db = db;
        _taskService = taskService;
        _env = env;
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

    public async Task<IActionResult> OnPostSubmitProofAsync([FromForm] int taskId, [FromForm] string proofUrl, [FromForm] IFormFile? proofFile)
    {
        try
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var userId = int.TryParse(userIdStr, out var id) ? id : 0;
            if (userId == 0)
                return new JsonResult(new { success = false, message = "Not logged in" });

            string? imagePath = null;
            if (proofFile is { Length: > 0 })
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "task_proofs");
                Directory.CreateDirectory(uploadsDir);

                var ext = Path.GetExtension(proofFile.FileName);
                var fileName = $"proof_{taskId}_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await proofFile.CopyToAsync(stream);

                imagePath = $"/uploads/task_proofs/{fileName}";
            }

            var result = await _taskService.SubmitProofAsync(taskId, proofUrl ?? "", imagePath ?? "", userId);
            return new JsonResult(new { success = result.IsSuccess, message = result.Message ?? "Done" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }
}
