using System;

namespace IMS.Core.Entities;

public class RoiContract
{
    public int Id { get; set; }
    public int InvestorId { get; set; }
    public Investor? InvestorNav { get; set; }
    public int ProjectId { get; set; }
    public Project? ProjectNav { get; set; }
    public decimal RoiAgreed { get; set; }
    public decimal MonthlyPayment { get; set; }
    public DateTime NextPaymentDate { get; set; }
    public string Status { get; set; } = "Active";
}
