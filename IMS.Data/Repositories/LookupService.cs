using IMS.Core.Entities;
using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Persistance.Repositories;

public class LookupService : ILookupService
{
    private readonly ApplicationDbContext _context;

    public LookupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<InvestmentInterest>> AllInvestmentInterests()
    {
        
        var invesses = _context.InvestmentInterests
                        .Select(static r => new InvIntRecord(r.Id, r.DisplayRange)).ToListAsync();

        return (IEnumerable<InvestmentInterest>)invesses;
    }

    public async Task<IEnumerable<InvestorType>> AllInvestorTypes()
    {
        var invites = _context.InvestorTypes
                      .Select(t => new InvType(t.Id,t.Name)).ToListAsync();

        return (IEnumerable<InvestorType>)invites;
    }
}

internal record InvIntRecord(int Id, string DisplayRange);
internal record InvType(int Id, string Name);
