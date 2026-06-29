using System;

namespace IMS.Core.Entities;

public class Payment
{
    public int PaymentId { get; set; }
    public int InvestorId { get; set; }
    public Investor? InvestorNav { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = "Completed";
}
