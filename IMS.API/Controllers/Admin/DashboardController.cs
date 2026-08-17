using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IMS.API.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IInvestorDocumentService _documentService;
    private readonly IMemoryCache _cache;
    private const string DashboardCacheKey = "DashboardStatsCacheKey";

    public DashboardController(ApplicationDbContext context, IInvestorDocumentService documentService, IMemoryCache cache)
    {
        _context = context;
        _documentService = documentService;
        _cache = cache;
    }

    [HttpGet("stats")]
    [ResponseCache(Duration = 15, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetDashboardStats()
    {
        if (_cache.TryGetValue(DashboardCacheKey, out object? cachedData) && cachedData != null)
        {
            return Ok(cachedData);
        }

        // Execute database reads sequentially since EF Core DbContext instance is not thread-safe for concurrent operations
        var investors = await _context.Investors.AsNoTracking().ToListAsync();
        var payments = await _context.Payments.AsNoTracking().ToListAsync();
        var documents = await _documentService.GetAllInvestorDocs();
        var roiContracts = await _context.RoiContracts.AsNoTracking().ToListAsync();
        var projects = await _context.Projects.AsNoTracking().ToListAsync();

        var result = new
        {
            investors,
            payments,
            documents,
            roiContracts,
            projects
        };

        _cache.Set(DashboardCacheKey, result, TimeSpan.FromSeconds(15));

        return Ok(result);
    }

    [HttpPost("clean-database")]
    [Authorize(Roles = "admin,Admin,superadmin,SuperAdmin")]
    public async Task<IActionResult> CleanDatabase()
    {
        // 1. Delete dependent transactional records
        await _context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM Payments;
            DELETE FROM InvestorDocuments;
            DELETE FROM InvestorCommitments;
            DELETE FROM RoiContracts;
            DELETE FROM SystemNotifications;
            DELETE FROM SystemReports;
            DELETE FROM Investors;
            
            -- Remove any investor/client users except Admin (tessma.cm@gmail.com) and Manager (imsmanager@yopmail.com)
            DELETE FROM AspNetUserRoles WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email NOT IN ('tessma.cm@gmail.com', 'imsmanager@yopmail.com'));
            DELETE FROM AspNetUserClaims WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email NOT IN ('tessma.cm@gmail.com', 'imsmanager@yopmail.com'));
            DELETE FROM AspNetUserLogins WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email NOT IN ('tessma.cm@gmail.com', 'imsmanager@yopmail.com'));
            DELETE FROM AspNetUserTokens WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email NOT IN ('tessma.cm@gmail.com', 'imsmanager@yopmail.com'));
            DELETE FROM AspNetUsers WHERE Email NOT IN ('tessma.cm@gmail.com', 'imsmanager@yopmail.com');
            
            -- Reset FundedAmount on projects
            UPDATE Projects SET FundedAmount = 0;
        ");

        _cache.Remove(DashboardCacheKey);

        return Ok(new { message = "Database cleaned successfully. All payments, documents, notifications, and investor data cleared." });
    }
}
