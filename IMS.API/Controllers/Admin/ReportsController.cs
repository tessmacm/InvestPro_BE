using IMS.Persistance.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.API.Controllers.Admin;

[Route("api/admin/reports")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var investorQuery = _context.Investors.AsQueryable();

        if (User.IsInRole("investor"))
        {
            var claim = User.FindFirst("investorId");
            if (claim != null && int.TryParse(claim.Value, out var id))
            {
                investorQuery = investorQuery.Where(i => i.InvestorId == id);
            }
            else
            {
                return Ok(new object[0]);
            }
        }

        var list = await investorQuery
            .SelectMany(i => _context.Projects.Select(p => new {
                investorId = i.InvestorId,
                investorName = i.LegalBusinessName ?? "Investor",
                projectId = p.Id,
                projectTitle = p.Title
            }))
            .Take(20)
            .ToListAsync();

        return Ok(list);
    }
}
