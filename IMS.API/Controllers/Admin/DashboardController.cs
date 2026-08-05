using IMS.Core.Interfaces;
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
    private readonly IInvestorDocumentService _documentService;

    public DashboardController(ApplicationDbContext context, IInvestorDocumentService documentService)
    {
        _context = context;
        _documentService = documentService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        // Execute database reads concurrently with Task.WhenAll and AsNoTracking for ultra-fast response
        var investorsTask = _context.Investors.AsNoTracking().ToListAsync();
        var paymentsTask = _context.Payments.AsNoTracking().ToListAsync();
        var documentsTask = _documentService.GetAllInvestorDocs();
        var roiContractsTask = _context.RoiContracts.AsNoTracking().ToListAsync();
        var projectsTask = _context.Projects.AsNoTracking().ToListAsync();

        await Task.WhenAll(investorsTask, paymentsTask, documentsTask, roiContractsTask, projectsTask);

        return Ok(new
        {
            investors = await investorsTask,
            payments = await paymentsTask,
            documents = await documentsTask,
            roiContracts = await roiContractsTask,
            projects = await projectsTask
        });
    }
}
