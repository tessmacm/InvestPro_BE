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

    private string? GetCurrentUserId()
    {
        return User.FindFirst("sub")?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? (Request.Headers.TryGetValue("x-user-id", out var uid) ? uid.ToString() : null);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? investorId)
    {
        var currentUserId = GetCurrentUserId();
        var isInvestor = User.IsInRole("investor") || User.IsInRole("Investor");

        var query = _context.SystemNotifications.Include(n => n.InvestorNav).AsQueryable();

        var investorsList = await _context.Investors.ToListAsync();
        var allUsers = await _context.Users.ToDictionaryAsync(u => u.Id, u => u);

        var allInvestors = investorsList
            .Where(i => i.InvestorId.HasValue)
            .GroupBy(i => i.InvestorId!.Value)
            .ToDictionary(g => g.Key, g => {
                var inv = g.First();
                if (!string.IsNullOrEmpty(inv.OwnerUserId) && allUsers.TryGetValue(inv.OwnerUserId, out var u))
                {
                    var fullName = $"{u.FirstName} {u.LastName}".Trim();
                    if (!string.IsNullOrWhiteSpace(fullName) && u.LastName != "User") return fullName;
                    if (!string.IsNullOrWhiteSpace(u.FirstName)) return u.FirstName;
                }
                return !string.IsNullOrEmpty(inv.LegalBusinessName) && inv.LegalBusinessName != "—" ? inv.LegalBusinessName : "Investor";
            });

        var userInvestorMap = investorsList
            .Where(i => !string.IsNullOrEmpty(i.OwnerUserId))
            .GroupBy(i => i.OwnerUserId!)
            .ToDictionary(g => g.Key, g => {
                var inv = g.First();
                if (allUsers.TryGetValue(g.Key, out var u))
                {
                    var fullName = $"{u.FirstName} {u.LastName}".Trim();
                    if (!string.IsNullOrWhiteSpace(fullName) && u.LastName != "User") return fullName;
                    if (!string.IsNullOrWhiteSpace(u.FirstName)) return u.FirstName;
                }
                return !string.IsNullOrEmpty(inv.LegalBusinessName) && inv.LegalBusinessName != "—" ? inv.LegalBusinessName : "Investor";
            });

        if (isInvestor)
        {
            var userInvestorIds = new List<int>();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                userInvestorIds = investorsList
                    .Where(i => i.OwnerUserId == currentUserId && i.InvestorId.HasValue)
                    .Select(i => i.InvestorId!.Value)
                    .ToList();
            }

            var claim = User.FindFirst("investorId");
            if (claim != null && int.TryParse(claim.Value, out var singleId) && !userInvestorIds.Contains(singleId))
            {
                userInvestorIds.Add(singleId);
            }

            var listAll = await query.OrderByDescending(n => n.CreatedAt).ToListAsync();

            // Strict in-memory check to ensure no false substring matching or broadcast leak
            var filteredList = listAll.Where(n =>
            {
                // 1. Sent by this investor
                if (!string.IsNullOrEmpty(currentUserId) && n.SenderUserId == currentUserId)
                    return true;

                // 2. Sent specifically to one of this investor's IDs
                if (n.InvestorId.HasValue && userInvestorIds.Contains(n.InvestorId.Value))
                    return true;

                // 3. Broadcast to all investors (only if from admin/manager and targetInvestorIds is null or "all")
                if (!n.InvestorId.HasValue && (string.IsNullOrEmpty(n.TargetInvestorIds) || n.TargetInvestorIds.Trim().Equals("all", StringComparison.OrdinalIgnoreCase)) && n.SenderRole != "investor")
                    return true;

                // 4. Targeted to multiple specific investors
                if (!string.IsNullOrEmpty(n.TargetInvestorIds) && !n.TargetInvestorIds.Trim().Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var ids = n.TargetInvestorIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out var parsed) ? parsed : 0)
                        .Where(id => id > 0);
                    if (ids.Any(id => userInvestorIds.Contains(id)))
                        return true;
                }

                return false;
            }).ToList();

            return Ok(filteredList.Select(n => MapNotificationDto(n, currentUserId, allInvestors, allUsers, userInvestorMap)));
        }
        else if (investorId.HasValue)
        {
            var targetId = investorId.Value;
            var listAll = await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
            var filteredList = listAll.Where(n =>
            {
                if (n.InvestorId == targetId) return true;
                if (!string.IsNullOrEmpty(n.TargetInvestorIds))
                {
                    var ids = n.TargetInvestorIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out var parsed) ? parsed : 0)
                        .Where(id => id > 0);
                    return ids.Contains(targetId);
                }
                return false;
            }).ToList();

            return Ok(filteredList.Select(n => MapNotificationDto(n, currentUserId, allInvestors, allUsers, userInvestorMap)));
        }
        else
        {
            var list = await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
            return Ok(list.Select(n => MapNotificationDto(n, currentUserId, allInvestors, allUsers, userInvestorMap)));
        }
    }

    private static object MapNotificationDto(
        SystemNotification n,
        string? currentUserId,
        Dictionary<int, string> allInvestors,
        Dictionary<string, ApplicationUser> allUsers,
        Dictionary<string, string> userInvestorMap)
    {
        string resolvedTo = "Management";
        if (n.SenderRole == "investor")
        {
            resolvedTo = "Admin & Manager";
        }
        else if (n.InvestorId.HasValue)
        {
            resolvedTo = allInvestors.TryGetValue(n.InvestorId.Value, out var name) ? name : "Investor";
        }
        else if (!string.IsNullOrEmpty(n.TargetInvestorIds) && !n.TargetInvestorIds.Trim().Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var targetIds = n.TargetInvestorIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                                               .Where(id => id > 0)
                                               .ToList();
            var names = targetIds.Select(id => allInvestors.TryGetValue(id, out var name) ? name : $"Inv#{id}");
            resolvedTo = string.Join(", ", names);
        }
        else
        {
            resolvedTo = "All Investors";
        }

        string resolvedSender = n.SenderName ?? "";
        if (n.SenderRole == "investor")
        {
            if (!string.IsNullOrEmpty(n.SenderUserId))
            {
                if (userInvestorMap.TryGetValue(n.SenderUserId, out var invName) && !string.IsNullOrEmpty(invName) && invName != "Investor")
                {
                    resolvedSender = invName;
                }
                else if (allUsers.TryGetValue(n.SenderUserId, out var u))
                {
                    var fullName = $"{u.FirstName} {u.LastName}".Trim();
                    resolvedSender = !string.IsNullOrWhiteSpace(fullName) && u.LastName != "User" ? fullName : (u.FirstName ?? u.Email ?? "Investor");
                }
            }
            if (string.IsNullOrWhiteSpace(resolvedSender) || resolvedSender.Equals("Investor", StringComparison.OrdinalIgnoreCase))
            {
                if (n.InvestorId.HasValue && allInvestors.TryGetValue(n.InvestorId.Value, out var invName) && invName != "Investor")
                {
                    resolvedSender = invName;
                }
            }
            if (string.IsNullOrWhiteSpace(resolvedSender))
            {
                resolvedSender = "Investor";
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(resolvedSender) || resolvedSender.Equals("Investor", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(n.SenderUserId) && allUsers.TryGetValue(n.SenderUserId, out var u))
                {
                    var fullName = $"{u.FirstName} {u.LastName}".Trim();
                    resolvedSender = !string.IsNullOrWhiteSpace(fullName) ? fullName : "Management";
                }
                else
                {
                    resolvedSender = "Management";
                }
            }
        }

        return new {
            id = n.Id,
            title = n.Title,
            message = n.Message,
            isRead = n.IsRead,
            readAt = n.ReadAt,
            createdAt = n.CreatedAt,
            senderUserId = n.SenderUserId,
            senderName = resolvedSender,
            senderRole = n.SenderRole ?? "admin",
            isSentByMe = !string.IsNullOrEmpty(currentUserId) && n.SenderUserId == currentUserId,
            investorId = n.InvestorId,
            targetInvestorIds = n.TargetInvestorIds,
            recipientName = resolvedTo,
            investorName = resolvedTo
        };
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var n = await _context.SystemNotifications.Include(n => n.InvestorNav).FirstOrDefaultAsync(x => x.Id == id);
        if (n == null) return NotFound();

        var currentUserId = GetCurrentUserId();
        var investorsList = await _context.Investors.ToListAsync();
        var allUsers = await _context.Users.ToDictionaryAsync(u => u.Id, u => u);

        var allInvestors = investorsList
            .Where(i => i.InvestorId.HasValue)
            .GroupBy(i => i.InvestorId!.Value)
            .ToDictionary(g => g.Key, g => {
                var inv = g.First();
                if (!string.IsNullOrEmpty(inv.OwnerUserId) && allUsers.TryGetValue(inv.OwnerUserId, out var u))
                {
                    var fullName = $"{u.FirstName} {u.LastName}".Trim();
                    if (!string.IsNullOrWhiteSpace(fullName) && u.LastName != "User") return fullName;
                    if (!string.IsNullOrWhiteSpace(u.FirstName)) return u.FirstName;
                }
                return !string.IsNullOrEmpty(inv.LegalBusinessName) && inv.LegalBusinessName != "—" ? inv.LegalBusinessName : "Investor";
            });

        var userInvestorMap = investorsList
            .Where(i => !string.IsNullOrEmpty(i.OwnerUserId))
            .GroupBy(i => i.OwnerUserId!)
            .ToDictionary(g => g.Key, g => {
                var inv = g.First();
                if (allUsers.TryGetValue(g.Key, out var u))
                {
                    var fullName = $"{u.FirstName} {u.LastName}".Trim();
                    if (!string.IsNullOrWhiteSpace(fullName) && u.LastName != "User") return fullName;
                    if (!string.IsNullOrWhiteSpace(u.FirstName)) return u.FirstName;
                }
                return !string.IsNullOrEmpty(inv.LegalBusinessName) && inv.LegalBusinessName != "—" ? inv.LegalBusinessName : "Investor";
            });

        return Ok(MapNotificationDto(n, currentUserId, allInvestors, allUsers, userInvestorMap));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SystemNotification model)
    {
        var currentUserId = GetCurrentUserId();
        var isInvestor = User.IsInRole("investor") || User.IsInRole("Investor");

        model.CreatedAt = DateTime.UtcNow;
        model.IsRead = false;
        model.ReadAt = null;
        model.SenderUserId = currentUserId;

        if (isInvestor)
        {
            model.SenderRole = "investor";
            string resolvedSenderName = "Investor";
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var user = await _context.Users.FindAsync(currentUserId);
                if (user != null)
                {
                    var fullName = $"{user.FirstName} {user.LastName}".Trim();
                    if (!string.IsNullOrWhiteSpace(fullName) && user.LastName != "User")
                    {
                        resolvedSenderName = fullName;
                    }
                    else if (!string.IsNullOrWhiteSpace(user.FirstName))
                    {
                        resolvedSenderName = user.FirstName;
                    }
                }

                if (resolvedSenderName == "Investor")
                {
                    var invProfile = await _context.Investors.FirstOrDefaultAsync(i => i.OwnerUserId == currentUserId);
                    if (invProfile != null && !string.IsNullOrEmpty(invProfile.LegalBusinessName) && invProfile.LegalBusinessName != "—")
                    {
                        resolvedSenderName = invProfile.LegalBusinessName;
                    }
                }
            }
            model.SenderName = resolvedSenderName;
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
                model.SenderName = user != null && !string.IsNullOrWhiteSpace($"{user.FirstName} {user.LastName}") 
                    ? $"{user.FirstName} {user.LastName}".Trim() 
                    : "Management";
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
    [HttpPost("{id}/read")]
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
