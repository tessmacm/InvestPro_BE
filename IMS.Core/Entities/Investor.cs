using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Core.Entities;

public class Investor
{
    public int? InvestorId { get; set; }

    // Cross-Layer Link: Stores the string Id from AspNetUsers
    public string? OwnerUserId { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? TaxIdOrSSN { get; set; } 
    public string? LegalBusinessName { get; set; } 
    public string? CompanyRegistrationNo { get; set; }  // EIN / Tax ID
    public string? AuthorizedSignerName { get; set; }
    public decimal? CapitalAmount { get; set; }
    public string? Notes { get; set; } 

    public int? InvestorTypeId { get; set; }
    public InvestorType? InvestorTypeNav { get; set; }

    public int? InvestmentInterestId { get; set; }
    public InvestmentInterest? InvestmentInterestNav { get; set; }

    public ICollection<InvestorCommitment> Commitments { get; set; } = [];
    public ICollection<InvestorDocument> Documents { get; set; } = [];
}
