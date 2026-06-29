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

[Route("api/admin/payments")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PaymentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? investorId)
    {
        var query = _context.Payments.Include(p => p.InvestorNav).AsQueryable();

        if (investorId.HasValue)
        {
            query = query.Where(p => p.InvestorId == investorId);
        }

        var list = await query.ToListAsync();
        return Ok(list.Select(p => new {
            paymentId = p.PaymentId,
            investorId = p.InvestorId,
            investorName = p.InvestorNav != null ? $"{p.InvestorNav.LegalBusinessName ?? "Investor"}" : "Investor",
            amount = p.Amount,
            paymentDate = p.PaymentDate,
            status = p.Status
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _context.Payments.Include(p => p.InvestorNav).FirstOrDefaultAsync(x => x.PaymentId == id);
        if (p == null) return NotFound();
        return Ok(new {
            paymentId = p.PaymentId,
            investorId = p.InvestorId,
            investorName = p.InvestorNav != null ? $"{p.InvestorNav.LegalBusinessName ?? "Investor"}" : "Investor",
            amount = p.Amount,
            paymentDate = p.PaymentDate,
            status = p.Status
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Payment model)
    {
        model.PaymentDate = DateTime.UtcNow;
        _context.Payments.Add(model);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = model.PaymentId }, model);
    }
}
