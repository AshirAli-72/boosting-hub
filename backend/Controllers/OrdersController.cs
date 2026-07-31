using System.Text.Json;
using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Models;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<OrdersController> _logger;
    private readonly IActivityLogService _activityLog;
    private readonly INotificationService _notificationService;

    public OrdersController(ApplicationDbContext db, ILogger<OrdersController> logger, IActivityLogService activityLog, INotificationService notificationService)
    {
        _db = db;
        _logger = logger;
        _activityLog = activityLog;
        _notificationService = notificationService;
    }

    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> SubmitOrder()
    {
        try
        {
        var fullName = Request.Form["fullName"].FirstOrDefault();
        var email = Request.Form["email"].FirstOrDefault();
        var platform = Request.Form["platform"].FirstOrDefault();
        var service = Request.Form["service"].FirstOrDefault();
        var quantityStr = Request.Form["quantity"].FirstOrDefault();
        var socialMediaUrl = Request.Form["socialMediaUrl"].FirstOrDefault();
        var totalAmountStr = Request.Form["totalAmount"].FirstOrDefault();
        var description = Request.Form["description"].FirstOrDefault();
        var currency = Request.Form["currency"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(platform) || string.IsNullOrWhiteSpace(service))
            return BadRequest(new { message = "Platform and Service are required." });

        int.TryParse(quantityStr, out var quantity);
        decimal.TryParse(totalAmountStr, out var totalAmount);

        if (quantity <= 0 && platform != "Contact")
            return BadRequest(new { message = "Quantity must be greater than zero." });

        if (totalAmount <= 0 && platform != "Contact")
            return BadRequest(new { message = "Total amount must be greater than zero." });

        var orderCurrency = (currency ?? "USD").Trim().ToUpperInvariant();

        string? attachmentPath = null;
        if (Request.Form.Files.Count > 0)
        {
            var file = Request.Form.Files[0];
            if (file.Length > 0)
            {
                var attachmentsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "attachments");
                Directory.CreateDirectory(attachmentsDir);

                var ext = Path.GetExtension(file.FileName);
                var fileName = $"order_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(attachmentsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                attachmentPath = $"uploads/attachments/{fileName}";
            }
        }

        var order = new Orders
        {
            FullName = fullName,
            Email = email,
            Platform = platform,
            Service = service,
            Quantity = quantity.ToString(),
            SocialMediaUrl = socialMediaUrl,
            TotalAmount = totalAmount,
            Currency = orderCurrency,
            Description = description,
            Attachment = attachmentPath,
            Status = StatusHelper.OrderPending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        _logger.LogInformation("New order #{OrderId} submitted (pending).", order.Id);

await _activityLog.LogAsync(
             userId: null, userName: fullName, userEmail: email,
             userRole: "Public", evt: "OrderSubmitted", description: $"New order #{order.Id} submitted by {fullName} ({email}) for {platform} - {service}. Awaiting admin review.",
             subjectType: "Order", subjectId: order.Id, subjectName: $"{platform} - {service}",
             newValues: JsonSerializer.Serialize(new { Platform = platform, Service = service, TotalAmount = totalAmount, Currency = orderCurrency }));

        try
        {
            var adminRoleIds = await _db.Roles
                .Where(r => r.RoleTitle != null && r.RoleTitle.Contains("Admin"))
                .Select(r => r.Id)
                .ToListAsync();

            var adminUserIds = await _db.UserHasRoles
                .Where(ur => adminRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            if (adminUserIds.Count == 0)
            {
                var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == "admin@gmail.com");
                if (adminUser != null)
                    adminUserIds.Add(adminUser.Id);
            }

            var notifications = adminUserIds.Select(adminId => new CreateNotificationDto
            {
                UserId = adminId,
                Type = "NewOrder",
                Title = "New Order Submitted",
                Message = $"Order #{order.Id} from {fullName} ({platform} - {service}) for {orderCurrency} {totalAmount:F2} is pending review.",
                Data = $"{{\"orderId\":{order.Id}}}"
            }).ToList();

            await _notificationService.CreateBulkNotificationAsync(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify admins about new order {OrderId}", order.Id);
        }

        return Ok(new { message = "Order submitted! Awaiting admin approval.", orderId = order.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SubmitOrder failed");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("draft")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> SaveDraftOrder()
    {
        var fullName = Request.Form["fullName"].FirstOrDefault();
        var email = Request.Form["email"].FirstOrDefault();
        var platform = Request.Form["platform"].FirstOrDefault();
        var service = Request.Form["service"].FirstOrDefault();
        var quantityStr = Request.Form["quantity"].FirstOrDefault();
        var socialMediaUrl = Request.Form["socialMediaUrl"].FirstOrDefault();
        var totalAmountStr = Request.Form["totalAmount"].FirstOrDefault();
        var description = Request.Form["description"].FirstOrDefault();
        var currency = Request.Form["currency"].FirstOrDefault();
        var paymentMethod = Request.Form["paymentMethod"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(platform) || string.IsNullOrWhiteSpace(service))
            return BadRequest(new { message = "Platform and Service are required." });

        int.TryParse(quantityStr, out var quantity);
        decimal.TryParse(totalAmountStr, out var totalAmount);

        var orderCurrency = (currency ?? "USD").Trim().ToUpperInvariant();

        string? attachmentPath = null;
        if (Request.Form.Files.Count > 0)
        {
            var file = Request.Form.Files[0];
            if (file.Length > 0)
            {
                var attachmentsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "attachments");
                if (!Directory.Exists(attachmentsDir))
                    Directory.CreateDirectory(attachmentsDir);

                var ext = Path.GetExtension(file.FileName);
                var fileName = $"order_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(attachmentsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                attachmentPath = $"uploads/attachments/{fileName}";
            }
        }

        var order = new Orders
        {
            FullName = fullName,
            Email = email,
            Platform = platform,
            Service = service,
            Quantity = quantity.ToString(),
            SocialMediaUrl = socialMediaUrl,
            TotalAmount = totalAmount,
            Currency = orderCurrency,
            Description = description,
            Attachment = attachmentPath,
            Status = StatusHelper.OrderDraft,
            CreatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        order.VoucherNo = GenerateVoucherNumber(order.Id);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Draft order #{OrderId} saved with voucher {VoucherNo}.", order.Id, order.VoucherNo);

        return Ok(new { message = "Draft saved.", orderId = order.Id, voucherNo = order.VoucherNo });
    }

    private static string GenerateVoucherNumber(int orderId)
    {
        return $"VCH-{DateTime.UtcNow:yyyy}-{orderId:D5}";
    }

    [HttpGet("lookup/{voucherNo}")]
    public async Task<IActionResult> LookupByVoucherNo(string voucherNo)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.VoucherNo == voucherNo && o.Status == StatusHelper.OrderDraft);

        if (order == null)
            return NotFound(new { message = "Voucher not found or already processed." });

        return Ok(new
        {
            order.Id,
            order.VoucherNo,
            order.FullName,
            order.Email,
            order.Platform,
            order.Service,
            order.Quantity,
            order.SocialMediaUrl,
            order.TotalAmount,
            order.Currency,
            order.Description,
            order.Attachment,
            order.CreatedAt
        });
    }

    [HttpPost("{id}/upload-paid-voucher")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadPaidVoucher(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "Order not found." });

        if (order.Status != StatusHelper.OrderDraft)
            return BadRequest(new { message = "Order is not in draft status." });

        var paymentMethod = Request.Form["paymentMethod"].FirstOrDefault();
        var paidAmountStr = Request.Form["paidAmount"].FirstOrDefault();

        decimal.TryParse(paidAmountStr, out var paidAmount);
        if (paidAmount <= 0) paidAmount = order.TotalAmount;

        string? voucherPath = null;
        if (Request.Form.Files.Count > 0)
        {
            var file = Request.Form.Files[0];
            if (file.Length > 0)
            {
                var vouchersDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "payment-vouchers");
                if (!Directory.Exists(vouchersDir))
                    Directory.CreateDirectory(vouchersDir);

                var ext = Path.GetExtension(file.FileName);
                var fileName = $"voucher_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(vouchersDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                voucherPath = $"uploads/payment-vouchers/{fileName}";
            }
        }

        order.Status = StatusHelper.OrderPending;

        var proof = new ManualPaymentProof
        {
            OrderId = id,
            PaidAmount = paidAmount,
            PaymentMethod = paymentMethod ?? "easypaisa",
            PaidVoucher = voucherPath,
            SubmitDate = DateTime.UtcNow,
            Status = StatusHelper.ManualPaymentPending
        };

        _db.ManualPaymentProofs.Add(proof);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Paid voucher uploaded for Draft Order #{OrderId}.", id);

        return Ok(new { message = "Payment proof submitted! Awaiting admin verification.", proofId = proof.Id });
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveOrder(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "Order not found." });

        if (order.Status != StatusHelper.OrderPending)
            return BadRequest(new { message = $"Order is already {StatusHelper.OrderStatusToString(order.Status)}." });

        order.Status = StatusHelper.OrderApproved;

        var pendingProofs = await _db.ManualPaymentProofs
            .Where(p => p.OrderId == id && p.Status == StatusHelper.ManualPaymentPending)
            .ToListAsync();
        foreach (var proof in pendingProofs)
        {
            proof.Status = StatusHelper.ManualPaymentPaid;
        }

        var quantity = int.TryParse(order.Quantity, out var q) && q > 0 ? q : 1;
        var tasksGenerated = GenerateTasksForOrder(order, quantity);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Order #{OrderId} approved. Generated {Count} tasks.", id, tasksGenerated);

await _activityLog.LogAsync(
             userId: null, userName: null, userEmail: null,
             userRole: "Admin", evt: "OrderApproved", description: $"Order #{id} approved by admin. {tasksGenerated} tasks generated for {order.Platform} - {order.Service}.",
             subjectType: "Order", subjectId: id, subjectName: $"{order.Platform} - {order.Service}",
             oldValues: JsonSerializer.Serialize(new { Status = StatusHelper.OrderStatusToString(StatusHelper.OrderPending) }),
             newValues: JsonSerializer.Serialize(new { Status = StatusHelper.OrderStatusToString(StatusHelper.OrderApproved), TasksGenerated = tasksGenerated }));

        try
        {
            var activeUserIds = await _db.Users
                .Where(u => u.Status == 1 && u.Email != "admin@gmail.com")
                .Select(u => u.Id)
                .ToListAsync();

            if (activeUserIds.Count > 0)
            {
                var rewardPerTask = quantity > 0 ? Math.Round(order.TotalAmount * 0.5m / quantity, 2) : 0m;
                var notifications = activeUserIds.Select(userId => new CreateNotificationDto
                {
                    UserId = userId,
                    Type = "NewTaskAvailable",
                    Title = "New Tasks Available",
                    Message = $"{tasksGenerated} new task(s) just added! {order.Platform} - {order.Service} ({rewardPerTask:F2} {order.Currency} per task). Grab them before they're gone!",
                    Data = $"{{\"orderId\":{order.Id},\"tasksGenerated\":{tasksGenerated}}}"
                }).ToList();

                await _notificationService.CreateBulkNotificationAsync(notifications);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify users about new tasks from order {OrderId}", id);
        }

        return Ok(new { message = $"Order approved. {tasksGenerated} task(s) generated and published.", tasksGenerated });
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectOrder(int id, [FromBody] RejectOrderDto dto)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "Order not found." });

        if (order.Status != StatusHelper.OrderPending)
            return BadRequest(new { message = $"Order is already {StatusHelper.OrderStatusToString(order.Status)}." });

        order.Status = StatusHelper.OrderRejected;
        order.RejectReason = dto.Reason;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Order #{OrderId} rejected. Reason: {Reason}", id, dto.Reason);

await _activityLog.LogAsync(
             userId: null, userName: null, userEmail: null,
             userRole: "Admin", evt: "OrderRejected", description: $"Order #{id} rejected. Reason: {dto.Reason}",
             subjectType: "Order", subjectId: id, subjectName: $"{order.Platform} - {order.Service}",
             oldValues: JsonSerializer.Serialize(new { Status = StatusHelper.OrderStatusToString(StatusHelper.OrderPending) }),
             newValues: JsonSerializer.Serialize(new { Status = StatusHelper.OrderStatusToString(StatusHelper.OrderRejected), RejectReason = dto.Reason }));

        return Ok(new { message = "Order has been rejected." });
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] string? status = null)
    {
        var query = _db.Orders.AsNoTracking()
            .Where(o => o.Status != StatusHelper.OrderDraft)
            .AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => StatusHelper.OrderStatusToString(o.Status) == status);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.FullName,
                o.Email,
                o.Platform,
                o.Service,
                o.Quantity,
                o.SocialMediaUrl,
                o.TotalAmount,
                o.Currency,
                o.Status,
                o.CreatedAt,
                o.Attachment,
                o.RejectReason,
                o.VoucherNo
            })
            .ToListAsync();

        return Ok(orders);
    }

    private int GenerateTasksForOrder(Orders order, int quantity)
    {
        var platforms = order.Platform.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var services = order.Service.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var userRewardPool = order.TotalAmount * 0.5m;
        var rewardPerCompletion = quantity > 0 ? Math.Round(userRewardPool / quantity, 2) : 0m;
        var expiryDate = DateTime.UtcNow.AddDays(5);

        var tasksGenerated = 0;
        foreach (var platform in platforms)
        {
            foreach (var service in services)
            {
                var taskGenerate = new TaskGenerate
                {
                    OrderId = order.Id,
                    Platform = platform,
                    Service = service,
                    Quantity = quantity,
                    Url = order.SocialMediaUrl ?? string.Empty,
                    Reward = rewardPerCompletion,
                    CreatedAt = DateTime.UtcNow,
                    ExpiryDate = expiryDate,
                    Status = StatusHelper.TaskGenerateActive
                };
                _db.TaskGenerates.Add(taskGenerate);
                tasksGenerated++;
            }
        }

        _db.SaveChanges();
        return tasksGenerated;
    }
}

public class RejectOrderDto
{
    public string Reason { get; set; } = string.Empty;
}
