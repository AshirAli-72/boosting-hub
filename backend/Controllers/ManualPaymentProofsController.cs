using BoostingHub.backend.Common;
using BoostingHub.backend.Data;
using BoostingHub.backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ManualPaymentProofsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ManualPaymentProofsController> _logger;

    public ManualPaymentProofsController(ApplicationDbContext db, ILogger<ManualPaymentProofsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> SubmitPaymentProof()
    {
        var orderIdStr = Request.Form["orderId"].FirstOrDefault();
        var paidAmountStr = Request.Form["paidAmount"].FirstOrDefault();
        var paymentMethod = Request.Form["paymentMethod"].FirstOrDefault();

        if (string.IsNullOrEmpty(orderIdStr) || !int.TryParse(orderIdStr, out var orderId))
            return BadRequest(new { message = "Invalid order ID." });

        if (string.IsNullOrEmpty(paidAmountStr) || !decimal.TryParse(paidAmountStr, out var paidAmount))
            return BadRequest(new { message = "Invalid paid amount." });

        if (string.IsNullOrEmpty(paymentMethod))
            return BadRequest(new { message = "Payment method is required." });

        var order = await _db.Orders.FindAsync(orderId);
        if (order == null)
            return NotFound(new { message = "Order not found." });

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
                var fileName = $"voucher_{orderId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(vouchersDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                voucherPath = $"uploads/payment-vouchers/{fileName}";
            }
        }

        var proof = new ManualPaymentProof
        {
            OrderId = orderId,
            PaidAmount = paidAmount,
            PaymentMethod = paymentMethod,
            PaidVoucher = voucherPath,
            SubmitDate = DateTime.UtcNow,
            Status = StatusHelper.ManualPaymentPending
        };

        _db.ManualPaymentProofs.Add(proof);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Payment proof submitted for Order #{OrderId}, ProofId={ProofId}", orderId, proof.Id);

        return Ok(new { message = "Payment proof submitted successfully. Awaiting admin verification.", proofId = proof.Id });
    }

    [HttpGet("by-order/{orderId}")]
    public async Task<IActionResult> GetByOrderId(int orderId)
    {
        var proof = await _db.ManualPaymentProofs
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.SubmitDate)
            .Select(p => new
            {
                p.Id,
                p.OrderId,
                p.PaidAmount,
                p.PaidVoucher,
                p.SubmitDate,
                p.PaymentMethod,
                p.Status
            })
            .FirstOrDefaultAsync();

        return Ok(proof);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? status = null)
    {
        var query = _db.ManualPaymentProofs
            .AsNoTracking()
            .Include(p => p.Order)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        var proofs = await query
            .OrderByDescending(p => p.SubmitDate)
            .Select(p => new
            {
                p.Id,
                p.OrderId,
                p.PaidAmount,
                p.PaidVoucher,
                p.SubmitDate,
                p.PaymentMethod,
                p.Status,
                OrderPlatform = p.Order.Platform,
                OrderService = p.Order.Service,
                OrderFullName = p.Order.FullName,
                OrderEmail = p.Order.Email,
                OrderAmount = p.Order.TotalAmount,
                OrderCurrency = p.Order.Currency
            })
            .ToListAsync();

        return Ok(proofs);
    }

    [HttpPost("{id}/verify")]
    public async Task<IActionResult> VerifyPayment(int id)
    {
        var proof = await _db.ManualPaymentProofs.FindAsync(id);
        if (proof == null)
            return NotFound(new { message = "Payment proof not found." });

        if (proof.Status != StatusHelper.ManualPaymentPending)
            return BadRequest(new { message = $"Payment proof is already {StatusHelper.ManualPaymentStatusToString(proof.Status)}." });

        proof.Status = StatusHelper.ManualPaymentPaid;

        var order = await _db.Orders.FindAsync(proof.OrderId);
        if (order != null && order.Status == StatusHelper.OrderPending)
        {
            order.Status = StatusHelper.OrderApproved;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Payment proof #{ProofId} verified. Order #{OrderId} approved.", id, proof.OrderId);

        return Ok(new { message = "Payment verified and order approved." });
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectPayment(int id)
    {
        var proof = await _db.ManualPaymentProofs.FindAsync(id);
        if (proof == null)
            return NotFound(new { message = "Payment proof not found." });

        if (proof.Status != StatusHelper.ManualPaymentPending)
            return BadRequest(new { message = $"Payment proof is already {StatusHelper.ManualPaymentStatusToString(proof.Status)}." });

        proof.Status = StatusHelper.ManualPaymentRejected;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Payment proof #{ProofId} rejected.", id);

        return Ok(new { message = "Payment proof rejected." });
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCounts()
    {
        var total = await _db.ManualPaymentProofs.CountAsync();
        var pending = await _db.ManualPaymentProofs.CountAsync(p => p.Status == StatusHelper.ManualPaymentPending);
        var paid = await _db.ManualPaymentProofs.CountAsync(p => p.Status == StatusHelper.ManualPaymentPaid);
        var rejected = await _db.ManualPaymentProofs.CountAsync(p => p.Status == StatusHelper.ManualPaymentRejected);

        return Ok(new { total, pending, paid, rejected });
    }

    [HttpGet("payment-accounts")]
    public async Task<IActionResult> GetPaymentAccounts()
    {
        var accounts = await _db.Accounts
            .AsNoTracking()
            .Where(a => a.Status == StatusHelper.AccountActive)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.AccountTitle,
                a.MobileNumber,
                a.AccountNumber,
                a.BankName
            })
            .ToListAsync();

        return Ok(accounts);
    }
}
