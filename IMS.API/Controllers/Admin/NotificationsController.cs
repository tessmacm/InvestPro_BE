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

[Route("api/admin/notifications")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? investorId)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isInvestor = User.IsInRole("investor") || User.IsInRole("Investor");

        var query = _context.SystemNotifications.Include(n => n.InvestorNav).AsQueryable();

        if (isInvestor)
        {
            var userInvestorIds = new List<int>();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                userInvestorIds = await _context.Investors
                    .Where(i => i.OwnerUserId == currentUserId && i.InvestorId.HasValue)
                    .Select(i => i.InvestorId!.Value)
                    .ToListAsync();
            }

            var claim = User.FindFirst("investorId");
            if (claim != null && int.TryParse(claim.Value, out var singleId) && !userInvestorIds.Contains(singleId))
            {
                userInvestorIds.Add(singleId);
            }

            // An investor sees:
            // 1. Notifications sent TO them (InvestorId in userInvestorIds or targetInvestorIds contains their ID or broadcast to all)
            // 2. Notifications sent BY them (SenderUserId == currentUserId)
            query = query.Where(n => 
                (n.SenderUserId == currentUserId) ||
                (n.InvestorId.HasValue && userInvestorIds.Contains(n.InvestorId.Value)) ||
                (n.InvestorId == null && string.IsNullOrEmpty(n.TargetInvestorIds) && n.SenderRole != "investor") ||
                (!string.IsNullOrEmpty(n.TargetInvestorIds) && userInvestorIds.Any(id => n.TargetInvestorIds.Contains("," + id + ",") || n.TargetInvestorIds.Contains(id.ToString())))
            );
        }
        else if (investorId.HasValue)
        {
            query = query.Where(n => n.InvestorId == investorId || n.TargetInvestorIds.Contains("," + investorId + ","));
        }

        var list = await query.OrderByDescending(n => n.CreatedAt).ToListAsync();

        var allInvestors = await _context.Investors
            .Include(i => i.OwnerUserId)
            .ToDictionaryAsync(i => i.InvestorId ?? 0, i => i.LegalBusinessName ?? "Investor");
        var allUsers = await _context.Users.ToDictionaryAsync(u => u.Id, u => u);

        return Ok(list.Select(n => {
            string resolvedTo = "Management";
            if (n.SenderRole == "investor")
            {
                resolvedTo = "Admin & Manager";
            }
            else if (n.InvestorId.HasValue)
            {
                resolvedTo = allInvestors.TryGetValue(n.InvestorId.Value, out var name) ? name : "Investor";
            }
            else if (!string.IsNullOrEmpty(n.TargetInvestorIds))
            {
                var targetIds = n.TargetInvestorIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(s => int.TryParse(s, out var id) ? id : 0)
                                                   .Where(id => id > 0)
                                                   .ToList();
                var names = targetIds.Select(id => allInvestors.TryGetValue(id, out var name) ? name : $"Inv#{id}");
                resolvedTo = string.Join(", ", names);
            }
            else
            {
                resolvedTo = "All Investors";
            }

            return new {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                isRead = n.IsRead,
                readAt = n.ReadAt,
                createdAt = n.CreatedAt,
                senderUserId = n.SenderUserId,
                senderName = n.SenderName ?? (n.SenderRole == "investor" ? "Investor" : "Management"),
                senderRole = n.SenderRole ?? "admin",
                isSentByMe = !string.IsNullOrEmpty(currentUserId) && n.SenderUserId == currentUserId,
                investorId = n.InvestorId,
                targetInvestorIds = n.TargetInvestorIds,
                recipientName = resolvedTo,
                investorName = resolvedTo
            };
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var n = await _context.SystemNotifications.Include(n => n.InvestorNav).FirstOrDefaultAsync(x => x.Id == id);
        if (n == null) return NotFound();

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var allInvestors = await _context.Investors.ToDictionaryAsync(i => i.InvestorId ?? 0, i => i.LegalBusinessName ?? "Investor");
        
        string resolvedTo = n.SenderRole == "investor" ? "Admin & Manager" : (n.InvestorId.HasValue && allInvestors.TryGetValue(n.InvestorId.Value, out var name) ? name : "All Investors");

        return Ok(new {
            id = n.Id,
            title = n.Title,
            message = n.Message,
            isRead = n.IsRead,
            readAt = n.ReadAt,
            createdAt = n.CreatedAt,
            senderUserId = n.SenderUserId,
            senderName = n.SenderName ?? "Management",
            senderRole = n.SenderRole ?? "admin",
            isSentByMe = !string.IsNullOrEmpty(currentUserId) && n.SenderUserId == currentUserId,
            investorId = n.InvestorId,
            targetInvestorIds = n.TargetInvestorIds,
            recipientName = resolvedTo,
            investorName = resolvedTo
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SystemNotification model)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isInvestor = User.IsInRole("investor") || User.IsInRole("Investor");

        model.CreatedAt = DateTime.UtcNow;
        model.IsRead = false;
        model.ReadAt = null;
        model.SenderUserId = currentUserId;

        if (isInvestor)
        {
            model.SenderRole = "investor";
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var user = await _context.Users.FindAsync(currentUserId);
                model.SenderName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Investor";
            }
            else
            {
                model.SenderName = "Investor";
            }
            // By default sent to Admin and Manager (InvestorId = null, target = null)
            model.InvestorId = null;
            model.TargetInvestorIds = null;
        }
        else
        {
            model.SenderRole = User.IsInRole("manager") ? "manager" : "admin";
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var user = await _context.Users.FindAsync(currentUserId);
                model.SenderName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Management";
            }
            else
            {
                model.SenderName = "Management";
            }
        }

        _context.SystemNotifications.Add(model);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SystemNotification model)
    {
        var n = await _context.SystemNotifications.FindAsync(id);
        if (n == null) return NotFound();

        n.Title = model.Title;
        n.Message = model.Message;
        if (!User.IsInRole("investor") && !User.IsInRole("Investor"))
        {
            n.InvestorId = model.InvestorId;
            n.TargetInvestorIds = model.TargetInvestorIds;
        }

        await _context.SaveChangesAsync();
        return Ok(n);
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var n = await _context.SystemNotifications.FindAsync(id);
        if (n == null) return NotFound();

        n.IsRead = true;
        n.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, isRead = n.IsRead, readAt = n.ReadAt });
    }
}
