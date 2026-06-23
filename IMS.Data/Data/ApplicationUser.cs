using IMS.Core.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Persistance.Data;

public class ApplicationUser : IdentityUser
{
    // Custom global properties for anyone who logs in
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property pointing to their financial investor profile (if they are an investor)
    public int? InvestorId { get; set; }
    public Investor? InvestorNav { get; set; }
}
