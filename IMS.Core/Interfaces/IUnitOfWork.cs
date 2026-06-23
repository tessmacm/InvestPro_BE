using IMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IAsyncRepository<InvestorType> InvestorTypes { get; }
    IAsyncRepository<InvestmentInterest> InvestmentInterests { get; }
    IAsyncRepository<Investor> Investors { get; }
    IAsyncRepository<Project> Projects { get; }
    IAsyncRepository<InvestorCommitment> Commitments { get; }

    //IAsyncRepository<InvestorProfile> InvestorProfiles { get; }
    //IAsyncRepository<IndividualProfile> IndividualProfiles { get; }
    //IAsyncRepository<BusinessProfile> BusinessProfiles { get; }
    //IAsyncRepository<Project> Projects { get; }
    Task<int> CompleteAsync(); // Commits all changes to the database and returns the number of affected rows
}
