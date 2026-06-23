using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Core.Interfaces;

public interface IInvestorManagementService
{
    Task<InvestorRegistrationResponse> RegisterAndCreateInvestorAsync(RegisterInvestorDTO dto);
    Task<IEnumerable<InvestorSummaryDTO>> GetAllInvestorsAsync();
    Task<bool> CreateInvestorProfileAsync(CreateInvestorProfileDTO dto);
    Task<bool> UpdateInvestorDetailsAsync(int profileId, UpdateInvestorDetailsDTO dto);
    Task<bool> DeleteInvestorProfileAsync(int profileId);
}

// Data Contracts defined completely inside Core
public class InvestorSummaryDTO
{
    public int InvestorId { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string InvestorTypeName { get; set; } = string.Empty;
    public string InvestmentRange { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateInvestorProfileDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int InvestorTypeId { get; set; }
    public int InvestmentInterestId { get; set; }
}

public class UpdateInvestorDetailsDTO
{
    public string Name { get; set; } = string.Empty;

    public string Organization { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public bool Status { get; set; }
}

public class RegisterInvestorDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class InvestorRegistrationResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    // Existing communication properties
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int InvestorId { get; set; }

    // New Verification Payload properties
    public string UserId { get; set; } = string.Empty;      // Required for verification link query string
    public string VerificationToken { get; set; } = string.Empty; // Holds the generated security token
}