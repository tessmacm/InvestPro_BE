using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace IMS.Core.Entities;

public class InvestorCommitment
{
    public int? Id { get; set; }
    public int? InvestorId { get; set; }
    public Investor? InvestorNav { get; set; }

    public int? ProjectId { get; set; }
    public Project? ProjectNav { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PrincipalAmount { get; set; }
    public DateTime? CommitmentDate { get; set; } = DateTime.UtcNow;
    public string? Status { get; set; } = "Active";
}
