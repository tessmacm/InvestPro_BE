using IMS.Persistance.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IMS.API.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? Request.Headers["x-user-role"].ToString();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Request.Headers["x-user-id"].ToString();
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var isInvestorUser = string.Equals(userRole, "investor", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(userRole, "client", StringComparison.OrdinalIgnoreCase);

        // Fetch datasets with AsNoTracking for maximum DB read performance
        var investors = await _context.Investors.AsNoTracking().ToListAsync();
        var payments = await _context.Payments.AsNoTracking().ToListAsync();
        var documents = await _context.InvestorDocuments.AsNoTracking().ToListAsync();
        var roiContracts = await _context.RoiContracts.AsNoTracking().ToListAsync();
        var projects = await _context.Projects.AsNoTracking().ToListAsync();

        return Ok(new
        {
            investors,
            payments,
            documents,
            roiContracts,
            projects
        });
    }
}
