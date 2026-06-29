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

[Route("api/admin/roi")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class RoiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RoiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _context.RoiContracts
            .Include(r => r.InvestorNav)
            .Include(r => r.ProjectNav)
            .ToListAsync();

        return Ok(list.Select(r => new {
            id = r.Id,
            investorId = r.InvestorId,
            investorName = r.InvestorNav != null ? $"{r.InvestorNav.LegalBusinessName ?? "Investor"}" : "Investor",
            projectId = r.ProjectId,
            projectTitle = r.ProjectNav != null ? r.ProjectNav.Title : "Project",
            roiAgreed = r.RoiAgreed,
            monthlyPayment = r.MonthlyPayment,
            nextPaymentDate = r.NextPaymentDate,
            status = r.Status
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var r = await _context.RoiContracts
            .Include(r => r.InvestorNav)
            .Include(r => r.ProjectNav)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r == null) return NotFound();

        return Ok(new {
            id = r.Id,
            investorId = r.InvestorId,
            investorName = r.InvestorNav != null ? $"{r.InvestorNav.LegalBusinessName ?? "Investor"}" : "Investor",
            projectId = r.ProjectId,
            projectTitle = r.ProjectNav != null ? r.ProjectNav.Title : "Project",
            roiAgreed = r.RoiAgreed,
            monthlyPayment = r.MonthlyPayment,
            nextPaymentDate = r.NextPaymentDate,
            status = r.Status
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RoiContract model)
    {
        _context.RoiContracts.Add(model);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
    }
}
