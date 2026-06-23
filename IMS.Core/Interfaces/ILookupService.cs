using IMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Core.Interfaces;

public interface ILookupService
{
    Task<IEnumerable<InvestorType>> AllInvestorTypes();
    Task<IEnumerable<InvestmentInterest>> AllInvestmentInterests();
}
