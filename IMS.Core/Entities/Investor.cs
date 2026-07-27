using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Core.Entities;

public class Investor
{
    public int? InvestorId { get; set; }

    // Cross-Layer Link: Stores the string Id from AspNetUsers
    public string? OwnerUserId { get; set; } 
    public DateTime? DateOfBirth { get; set; }
    public string? TaxIdOrSSN { get; set; } 
    public string? LegalBusinessName { get; set; } 
    public string? CompanyRegistrationNo { get; set; }  // EIN / Tax ID
    public string? AuthorizedSignerName { get; set; }
    public decimal? CapitalAmount { get; set; }
    public string? Notes { get; set; } 
    public DateTime? DateOfBoarding { get; set; }

    // Minimum ROI Selection Tracking
    public int? MinRoiRangeId { get; set; }
    public RoiRange? MinRoiRangeNav { get; set; }

    // Maximum ROI Selection Tracking
    public int? MaxRoiRangeId { get; set; }
    public RoiRange? MaxRoiRangeNav { get; set; }

    // ROI Type Selection Tracking
    public int? RoiTypeId { get; set; }
    public RoiType? RoiTypeNav { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNo { get; set; }
    public string? SortCode { get; set; }
    public string? Address { get; set; }
    public string? Witness { get; set; }
    public string? PayoutType { get; set; }
    public int? ProjectId { get; set; }
    public int? InvestorTypeId { get; set; }
    public InvestorType? InvestorTypeNav { get; set; }
    public bool IsAgreedToTerms { get; set; } = false;

    //public int? InvestmentInterestId { get; set; }
    //public InvestmentInterest? InvestmentInterestNav { get; set; }

    public ICollection<InvestorCommitment> Commitments { get; set; } = [];
    public ICollection<InvestorDocument> Documents { get; set; } = [];
}
