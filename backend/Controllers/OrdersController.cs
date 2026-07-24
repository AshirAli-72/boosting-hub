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

    // POST /api/orders � submit requirements from landing page
    [HttpPost]
    public async Task<IActionResult> SubmitOrder([FromBody] SubmitOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Platform) || string.IsNullOrWhiteSpace(dto.Service))
            return BadRequest(new { message = "Platform and Service are required." });

        var pkg = await _db.Packages.FindAsync(dto.PackageId);
        if (pkg == null)
            return BadRequest(new { message = "Selected package not found." });

        var orderCurrency = string.IsNullOrWhiteSpace(dto.Currency) ? "PKR" : dto.Currency.ToUpperInvariant();

        var order = new Orders
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Platform = dto.Platform,
            Service = dto.Service,
            Quantity = dto.Quantity.ToString(),
            SocialMediaUrl = dto.SocialMediaUrl,
            PackageId = pkg.Id,
            Currency = orderCurrency,
            Description = dto.Description,
            Status = StatusHelper.OrderApproved,
            CreatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Auto-generate tasks for this order
        var platforms = dto.Platform.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var services = dto.Service.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var tasksGenerated = 0;
        var totalOrderValue = pkg.Price * dto.Quantity;
        var userRewardPool = totalOrderValue * 0.5m;
        var rewardPerCompletion = dto.Quantity > 0 ? Math.Round(userRewardPool / dto.Quantity, 2) : 0m;
        var expiryDate = DateTime.UtcNow.AddDays(3);

        foreach (var platform in platforms)
        {
            foreach (var service in services)
            {
                var taskGenerate = new TaskGenerate
                {
                    OrderId = order.Id,
                    Platform = platform,
                    Service = service,
                    Quantity = dto.Quantity,
                    Url = dto.SocialMediaUrl ?? string.Empty,
                    Reward = rewardPerCompletion,
                    Currency = "PKR",
                    CreatedAt = DateTime.UtcNow,
                    ExpiryDate = expiryDate,
                    Status = StatusHelper.TaskGenerateActive
                };
                _db.TaskGenerates.Add(taskGenerate);
                tasksGenerated++;
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("New order #{OrderId} submitted and auto-approved. {Count} tasks generated.", order.Id, tasksGenerated);

        await _activityLog.LogAsync(
            userId: null, userName: dto.FullName, userEmail: dto.Email,
            userRole: "Public", evt: "OrderPaid", description: $"Order #{order.Id} paid and auto-approved by {dto.FullName} ({dto.Email}). {tasksGenerated} tasks generated.",
            subjectType: "Order", subjectId: order.Id, subjectName: $"{dto.Platform} - {dto.Service}",
            newValues: JsonSerializer.Serialize(new { Platform = dto.Platform, Service = dto.Service, PackageId = pkg.Id, PackagePrice = pkg.Price, TasksGenerated = tasksGenerated }),
            httpContext: HttpContext);

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
                Title = "New Order Paid",
                Message = $"Order #{order.Id} from {dto.FullName} ({dto.Platform} - {dto.Service}) for Rs. {totalOrderValue:F2} has been paid. {tasksGenerated} tasks auto-generated.",
                Data = $"{{\"orderId\":{order.Id}}}"
            }).ToList();

            await _notificationService.CreateBulkNotificationAsync(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify admins about new order {OrderId}", order.Id);
        }

        return Ok(new { message = "Payment confirmed! Your order is now active.", orderId = order.Id, tasksGenerated });
    }

    // POST /api/orders/{id}/approve � admin approves the order and generates tasks
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveOrder(int id, [FromBody] ApproveOrderDto dto)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "Order not found." });

        if (order.Status != StatusHelper.OrderPending)
            return BadRequest(new { message = $"Order is already {order.Status}." });

        order.Status = StatusHelper.OrderApproved;

        var platforms = order.Platform.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var services = order.Service.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var numberOfTasks = platforms.Length * services.Length;
        var quantity = int.TryParse(order.Quantity, out var q) && q > 0 ? q : 1;

        var pkg = order.PackageId.HasValue ? await _db.Packages.FindAsync(order.PackageId.Value) : null;
        var perUnitPrice = pkg?.Price ?? 0m;
        var totalOrderValue = perUnitPrice * quantity;

        var userRewardPool = totalOrderValue * 0.5m;
        var totalCompletions = numberOfTasks * quantity;
        var rewardPerCompletion = totalCompletions > 0 ? Math.Round(userRewardPool / totalCompletions, 2) : 0m;
        var expiryDate = DateTime.UtcNow.AddDays(3);

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
                    Currency = "PKR",
                    CreatedAt = DateTime.UtcNow,
                    ExpiryDate = expiryDate,
                    Status = StatusHelper.TaskGenerateActive
                };
                _db.TaskGenerates.Add(taskGenerate);
                tasksGenerated++;
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Order #{OrderId} approved. Generated {Count} tasks.", id, tasksGenerated);

        await _activityLog.LogAsync(
            userId: null, userName: null, userEmail: null,
            userRole: "Admin", evt: "OrderApproved", description: $"Order #{id} approved, {tasksGenerated} tasks generated",
            subjectType: "Order", subjectId: id, subjectName: $"{order.Platform} - {order.Service}",
            oldValues: JsonSerializer.Serialize(new { Status = StatusHelper.OrderStatusToString(StatusHelper.OrderPending) }),
            newValues: JsonSerializer.Serialize(new { Status = StatusHelper.OrderStatusToString(StatusHelper.OrderApproved), TasksGenerated = tasksGenerated }),
            httpContext: HttpContext);

        try
        {
            var activeUserIds = await _db.Users
                .Where(u => u.Status == 1 && u.Email != "admin@gmail.com")
                .Select(u => u.Id)
                .ToListAsync();

            if (activeUserIds.Count > 0)
            {
                var notifications = activeUserIds.Select(userId => new CreateNotificationDto
                {
                    UserId = userId,
                    Type = "NewTaskAvailable",
                    Title = "New Tasks Available",
                    Message = $"{tasksGenerated} new task(s) just added! {order.Platform} - {order.Service} (Rs. {rewardPerCompletion:F2} per task). Grab them before they're gone!",
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

    // POST /api/orders/{id}/reject � admin rejects the order
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectOrder(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "Order not found." });

        if (order.Status != StatusHelper.OrderPending)
            return BadRequest(new { message = $"Order is already {order.Status}." });

        order.Status = StatusHelper.OrderRejected;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Order #{OrderId} rejected.", id);

        await _activityLog.LogAsync(
            userId: null, userName: null, userEmail: null,
            userRole: "Admin", evt: "OrderRejected", description: $"Order #{id} rejected",
            subjectType: "Order", subjectId: id, subjectName: $"{order.Platform} - {order.Service}",
            oldValues: JsonSerializer.Serialize(new { Status = StatusHelper.OrderStatusToString(StatusHelper.OrderPending) }),
            newValues: JsonSerializer.Serialize(new { Status = StatusHelper.OrderStatusToString(StatusHelper.OrderRejected) }),
            httpContext: HttpContext);

        return Ok(new { message = "Order has been rejected." });
    }

    // GET /api/orders � list all orders for admin
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] string? status = null)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();
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
                o.PackageId,
                o.Status,
                o.CreatedAt
            })
            .ToListAsync();

        return Ok(orders);
    }
}

public class ApproveOrderDto
{
    public decimal? RewardPerTask { get; set; }
}
