using IMS.Core.Entities;
using IMS.Persistance.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.API.Controllers.Admin;

[Route("api/admin/payments")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PaymentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? investorId)
    {
        var query = _context.Payments.AsNoTracking()
            .Include(p => p.InvestorNav)
            .AsQueryable();

        if (User.IsInRole("investor") || User.IsInRole("Investor") || User.IsInRole("client") || User.IsInRole("Client"))
        {
            var userId = User.FindFirst("sub")?.Value 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst("name")?.Value 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var investorIdClaim = User.FindFirst("investorId")?.Value;

            // Find all Investor contract profiles belonging to this authenticated user (by matching OwnerUserId or Owner User's Email)
            var userInvestorIds = await (
                from inv in _context.Investors.AsNoTracking()
                join u in _context.Users.AsNoTracking() on inv.OwnerUserId equals u.Id into userGroup
                from u in userGroup.DefaultIfEmpty()
                where inv.InvestorId.HasValue && (
                    (!string.IsNullOrEmpty(userId) && inv.OwnerUserId == userId) ||
                    (!string.IsNullOrEmpty(email) && u != null && u.Email == email)
                )
                select inv.InvestorId!.Value
            ).ToListAsync();

            if (userInvestorIds.Any())
            {
                query = query.Where(p => userInvestorIds.Contains(p.InvestorId));
            }
            else if (!string.IsNullOrEmpty(investorIdClaim) && int.TryParse(investorIdClaim, out var parsedId))
            {
                query = query.Where(p => p.InvestorId == parsedId);
            }
            else
            {
                return Ok(new object[0]);
            }
        }
        else if (investorId.HasValue)
        {
            query = query.Where(p => p.InvestorId == investorId);
        }

        var list = await query.ToListAsync();

        // Preload users for O(1) in-memory lookup
        var usersDict = await _context.Users.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u);

        return Ok(list.Select(p => {
            var inv = p.InvestorNav;
            var user = inv != null && !string.IsNullOrEmpty(inv.OwnerUserId) && usersDict.TryGetValue(inv.OwnerUserId, out var u) ? u : null;
            var name = user != null ? (user.LastName == "User" || string.IsNullOrWhiteSpace(user.LastName) ? user.FirstName : $"{user.FirstName} {user.LastName}".Trim()) : (inv?.LegalBusinessName ?? "Investor");
            if (string.IsNullOrEmpty(name)) name = inv?.LegalBusinessName ?? "Investor";

            var cycle = inv?.PayoutType == "Fixed" || inv?.RoiTypeId == 1 ? "Constant" : (inv?.RoiTypeId == 2 ? "Weekly" : (inv?.RoiTypeId == 4 ? "Quarterly" : (inv?.RoiTypeId == 6 ? "Half-Yearly" : (inv?.RoiTypeId == 5 ? "Yearly" : "Monthly"))));

            return new {
                paymentId = p.PaymentId,
                investorId = p.InvestorId,
                investorName = name,
                investorEmail = user?.Email ?? "",
                mobile = user?.PhoneNumber ?? "",
                amount = p.Amount,
                paymentDate = p.PaymentDate,
                dueDate = p.PaymentDate.ToString("yyyy-MM-dd"),
                paymentCycle = cycle,
                status = p.Status,
                isSent = p.IsSent,
                isReceived = p.IsReceived
            };
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _context.Payments.Include(p => p.InvestorNav).FirstOrDefaultAsync(x => x.PaymentId == id);
        if (p == null) return NotFound();

        var inv = p.InvestorNav;
        var user = inv != null && !string.IsNullOrEmpty(inv.OwnerUserId) ? await _context.Users.FindAsync(inv.OwnerUserId) : null;
        var name = user != null ? (user.LastName == "User" || string.IsNullOrWhiteSpace(user.LastName) ? user.FirstName : $"{user.FirstName} {user.LastName}".Trim()) : (inv?.LegalBusinessName ?? "Investor");
        if (string.IsNullOrEmpty(name)) name = inv?.LegalBusinessName ?? "Investor";

        var cycle = inv?.PayoutType == "Fixed" || inv?.RoiTypeId == 1 ? "Constant" : (inv?.RoiTypeId == 2 ? "Weekly" : (inv?.RoiTypeId == 4 ? "Quarterly" : (inv?.RoiTypeId == 5 ? "Yearly" : "Monthly")));

        return Ok(new {
            paymentId = p.PaymentId,
            investorId = p.InvestorId,
            investorName = name,
            investorEmail = user?.Email ?? "",
            mobile = user?.PhoneNumber ?? "",
            amount = p.Amount,
            paymentDate = p.PaymentDate,
            dueDate = p.PaymentDate.ToString("yyyy-MM-dd"),
            paymentCycle = cycle,
            status = p.Status,
            isSent = p.IsSent,
            isReceived = p.IsReceived
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Payment model)
    {
        model.PaymentDate = DateTime.UtcNow;
        _context.Payments.Add(model);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = model.PaymentId }, model);
    }

    [HttpPost("{id}/acknowledge-sent")]
    [Authorize(Policy = "ElevatedOrManager")]
    public async Task<IActionResult> AcknowledgeSent(int id)
    {
        var p = await _context.Payments.FindAsync(id);
        if (p == null) return NotFound();

        p.IsSent = true;
        p.Status = p.IsReceived ? "Received" : "Sent";
        await _context.SaveChangesAsync();
        return Ok(new { success = true, status = p.Status });
    }

    [HttpPost("{id}/acknowledge-received")]
    public async Task<IActionResult> AcknowledgeReceived(int id)
    {
        var p = await _context.Payments.FindAsync(id);
        if (p == null) return NotFound();

        if (User.IsInRole("investor") || User.IsInRole("Investor"))
        {
            var claim = User.FindFirst("investorId");
            if (claim == null || !int.TryParse(claim.Value, out var invId) || p.InvestorId != invId)
            {
                return Forbid();
            }
        }

        if (!p.IsSent)
        {
            return BadRequest(new { message = "Cannot acknowledge receipt before payment is acknowledged as sent by admin." });
        }

        p.IsReceived = true;
        p.Status = "Received";
        await _context.SaveChangesAsync();
        return Ok(new { success = true, status = p.Status });
    }
}
