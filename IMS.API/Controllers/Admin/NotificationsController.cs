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

        if (User.IsInRole("investor"))
        {
            var claim = User.FindFirst("investorId");
            if (claim != null && int.TryParse(claim.Value, out var id))
            {
                query = query.Where(n => n.InvestorId == id || n.InvestorId == null);
            }
            else
            {
                return Ok(new object[0]);
            }
        }
        else if (investorId.HasValue)
        {
            query = query.Where(n => n.InvestorId == investorId);
        }

        var list = await query.ToListAsync();
        return Ok(list.Select(n => new {
            id = n.Id,
            title = n.Title,
            message = n.Message,
            eventType = n.EventType,
            isRead = n.IsRead,
            createdAt = n.CreatedAt,
            investorId = n.InvestorId,
            investorName = n.InvestorNav != null ? $"{n.InvestorNav.LegalBusinessName ?? "Investor"}" : "All Investors",
            status = n.Status
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var n = await _context.SystemNotifications.Include(n => n.InvestorNav).FirstOrDefaultAsync(x => x.Id == id);
        if (n == null) return NotFound();
        return Ok(new {
            id = n.Id,
            title = n.Title,
            message = n.Message,
            eventType = n.EventType,
            isRead = n.IsRead,
            createdAt = n.CreatedAt,
            investorId = n.InvestorId,
            investorName = n.InvestorNav != null ? $"{n.InvestorNav.LegalBusinessName ?? "Investor"}" : "All Investors",
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
