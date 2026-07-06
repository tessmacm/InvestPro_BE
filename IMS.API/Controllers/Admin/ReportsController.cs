using IMS.Core.Entities;
using IMS.Persistance.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IMS.API.Controllers.Admin;

[Route("api/admin/reports")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = _context.SystemReports.AsQueryable();

        if (User.IsInRole("investor") || User.IsInRole("Investor"))
        {
            var user = await _userManager.GetUserAsync(User);
            var investorIdStr = (user?.InvestorId ?? 0).ToString();
            
            var allReports = await query.ToListAsync();
            // Filter in memory for complex comma search
            var filtered = allReports.Where(r => 
                r.TargetInvestorIds == "all" || 
                r.TargetInvestorIds.Split(',', StringSplitOptions.TrimEntries).Contains(investorIdStr)
            ).ToList();

            return Ok(filtered.Select(r => new {
                id = r.Id,
                title = r.Title,
                type = r.Type,
                size = r.Size,
                url = r.Url,
                uploadedBy = r.UploadedBy,
                createdAt = r.CreatedAt,
                targetInvestorIds = r.TargetInvestorIds,
                investorName = "My Report"
            }));
        }

        var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        var investorsList = await _context.Investors.ToListAsync();
        var investorsDict = investorsList.ToDictionary(
            i => (i.InvestorId ?? 0).ToString(), 
            i => i.LegalBusinessName ?? "Investor"
        );

        var resolved = list.Select(r => {
            string targetNames = "All Investors";
            if (r.TargetInvestorIds != "all")
            {
                var ids = r.TargetInvestorIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var names = ids.Select(id => investorsDict.ContainsKey(id) ? investorsDict[id] : "Unknown").ToList();
                targetNames = names.Any() ? string.Join(", ", names) : "No targets";
            }

            return new {
                id = r.Id,
                title = r.Title,
                type = r.Type,
                size = r.Size,
                url = r.Url,
                uploadedBy = r.UploadedBy,
                createdAt = r.CreatedAt,
                targetInvestorIds = r.TargetInvestorIds,
                investorName = targetNames
            };
        });

        return Ok(resolved);
    }

    [HttpPost]
    [Authorize(Policy = "ElevatedOrManager")]
    public async Task<IActionResult> Create([FromBody] UploadReportDTO dto)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var report = new SystemReport
        {
            Title = dto.Title,
            Type = dto.Type,
            Size = dto.Size,
            Url = dto.Url,
            UploadedBy = adminUserId ?? "System Admin",
            CreatedAt = DateTime.UtcNow,
            TargetInvestorIds = string.IsNullOrEmpty(dto.TargetInvestorIds) ? "all" : dto.TargetInvestorIds
        };

        _context.SystemReports.Add(report);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Report uploaded successfully." });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "ElevatedOrManager")]
    public async Task<IActionResult> Delete(int id)
    {
        var report = await _context.SystemReports.FindAsync(id);
        if (report == null) return NotFound(new { message = "Report not found." });

        _context.SystemReports.Remove(report);
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }
}

public class UploadReportDTO
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "PDF";
    public string Size { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string TargetInvestorIds { get; set; } = "all";
}
