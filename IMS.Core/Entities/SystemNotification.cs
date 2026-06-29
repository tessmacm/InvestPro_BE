using System;

namespace IMS.Core.Entities;

public class SystemNotification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? InvestorId { get; set; }
    public Investor? InvestorNav { get; set; }
    public string Status { get; set; } = "Active";
}
