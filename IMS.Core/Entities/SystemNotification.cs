using System;

namespace IMS.Core.Entities;

public class SystemNotification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? EventType { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? SenderUserId { get; set; }
    public string? SenderName { get; set; }
    public string? SenderRole { get; set; }
    public int? InvestorId { get; set; }
    public Investor? InvestorNav { get; set; }
    public string? TargetInvestorIds { get; set; }
    public string? Status { get; set; }
}
