using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace IMS.Core.Entities;

public class Project
{
    public int? Id { get; set; }
    public string? Title { get; set; } 
    public string? Description { get; set; }
    public decimal? TargetFunding { get; set; }
    public decimal? FundedAmount { get; set; } = 0;
    public DateTime? LaunchDate { get; set; }
    public string? Status { get; set; } = "Draft"; // Draft, Open, FullyFunded, Closed

    public ICollection<InvestorCommitment>? Commitments { get; set; } = [];
}
