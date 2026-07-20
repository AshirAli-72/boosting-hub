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

    public OrdersController(ApplicationDbContext db, ILogger<OrdersController> logger, IActivityLogService activityLog)
    {
        _db = db;
        _logger = logger;
        _activityLog = activityLog;
    }

    // POST /api/orders � submit requirements from landing page
    [HttpPost]
    public async Task<IActionResult> SubmitOrder([FromBody] SubmitOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Platform) || string.IsNullOrWhiteSpace(dto.Service))
            return BadRequest(new { message = "Platform and Service are required." });

        var order = new Orders
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Platform = dto.Platform,
            Service = dto.Service,
            Quantity = dto.Quantity.ToString(),
            SocialMediaUrl = dto.SocialMediaUrl,
            Budget = dto.Budget,
            Currency = dto.Currency,
            Description = dto.Description,
            Status = StatusHelper.OrderPending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        _logger.LogInformation("New order #{OrderId} submitted from {Email}", order.Id, order.Email);

        await _activityLog.LogAsync(
            userId: null, userName: dto.FullName, userEmail: dto.Email,
            userRole: "Public", evt: "OrderSubmitted", description: $"New order #{order.Id} submitted by {dto.FullName} ({dto.Email})",
            subjectType: "Order", subjectId: order.Id, subjectName: $"{dto.Platform} - {dto.Service}",
            newValues: JsonSerializer.Serialize(new { Platform = dto.Platform, Service = dto.Service, Budget = dto.Budget, Currency = dto.Currency }),
            httpContext: HttpContext);

        return Ok(new { message = "Your requirements have been submitted successfully!", orderId = order.Id });
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

        var userRewardPool = order.Budget * 0.5m;
        var rewardPerCompletion = numberOfTasks > 0 ? Math.Round(userRewardPool / numberOfTasks, 2) : 0m;

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
                    Quantity = 1,
                    Url = order.SocialMediaUrl ?? string.Empty,
                    Reward = rewardPerCompletion,
                    Currency = order.Currency,
                    CreatedAt = DateTime.UtcNow,
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
                o.Budget,
                o.Currency,
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
