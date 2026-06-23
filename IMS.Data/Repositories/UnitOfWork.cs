using IMS.Core.Entities;
using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Persistance.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public IAsyncRepository<InvestorType> InvestorTypes { get; }
    public IAsyncRepository<InvestmentInterest> InvestmentInterests { get; }
    public IAsyncRepository<Investor> Investors { get; }
    public IAsyncRepository<Project> Projects { get; }
    public IAsyncRepository<InvestorCommitment> Commitments {  get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        InvestorTypes = new EfRepository<InvestorType>(_context);
        InvestmentInterests = new EfRepository<InvestmentInterest>(_context);
        Investors = new EfRepository<Investor>(_context);
        Projects = new EfRepository<Project>(_context);
        Commitments = new EfRepository<InvestorCommitment>(_context);
    }

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
