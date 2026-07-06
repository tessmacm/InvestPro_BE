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
        var query = _context.SystemNotifications.Include(n => n.InvestorNav).AsQueryable();

        if (User.IsInRole("investor") || User.IsInRole("Investor"))
        {
            var claim = User.FindFirst("investorId");
            if (claim != null && int.TryParse(claim.Value, out var id))
            {
                query = query.Where(n => n.InvestorId == id || 
                                         (n.InvestorId == null && n.TargetInvestorIds == null) ||
                                         n.TargetInvestorIds.Contains("," + id + ","));
            }
            else
            {
                return Ok(new object[0]);
            }
        }
        else if (investorId.HasValue)
        {
            query = query.Where(n => n.InvestorId == investorId || n.TargetInvestorIds.Contains("," + investorId + ","));
        }

        var list = await query.ToListAsync();
        var allInvestors = await _context.Investors.ToDictionaryAsync(i => i.InvestorId ?? 0, i => i.LegalBusinessName ?? "Investor");

        return Ok(list.Select(n => {
            string resolvedTo = "All Investors";
            if (n.InvestorId.HasValue)
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

            return new {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                eventType = n.EventType,
                isRead = n.IsRead,
                createdAt = n.CreatedAt,
                investorId = n.InvestorId,
                targetInvestorIds = n.TargetInvestorIds,
                investorName = resolvedTo,
                status = n.Status
            };
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var n = await _context.SystemNotifications.Include(n => n.InvestorNav).FirstOrDefaultAsync(x => x.Id == id);
        if (n == null) return NotFound();

        var allInvestors = await _context.Investors.ToDictionaryAsync(i => i.InvestorId ?? 0, i => i.LegalBusinessName ?? "Investor");
        string resolvedTo = "All Investors";
        if (n.InvestorId.HasValue)
        {
            resolvedTo = allInvestors.TryGetValue(n.InvestorId.Value, out var name) ? name : "Investor";
        }
        else if (!string.IsNullOrEmpty(n.TargetInvestorIds))
        {
            var targetIds = n.TargetInvestorIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(s => int.TryParse(s, out var id) ? id : 0)
                                               .Where(id => id > 0)
                                               .ToList();
            var names = targetIds.Select(tid => allInvestors.TryGetValue(tid, out var name) ? name : $"Inv#{tid}");
            resolvedTo = string.Join(", ", names);
        }

        return Ok(new {
            id = n.Id,
            title = n.Title,
            message = n.Message,
            eventType = n.EventType,
            isRead = n.IsRead,
            createdAt = n.CreatedAt,
            investorId = n.InvestorId,
            targetInvestorIds = n.TargetInvestorIds,
            investorName = resolvedTo,
            status = n.Status
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SystemNotification model)
    {
        model.CreatedAt = DateTime.UtcNow;
        model.IsRead = false;
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
        n.EventType = model.EventType;
        n.IsRead = model.IsRead;
        n.Status = model.Status;
        n.InvestorId = model.InvestorId;
        n.TargetInvestorIds = model.TargetInvestorIds;

        await _context.SaveChangesAsync();
        return Ok(n);
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var n = await _context.SystemNotifications.FindAsync(id);
        if (n == null) return NotFound();

        n.IsRead = true;
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }
}
