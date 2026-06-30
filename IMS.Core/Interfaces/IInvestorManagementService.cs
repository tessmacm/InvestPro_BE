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

public class InvestorSummaryDTO
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string mobile { get; set; } = string.Empty;
    public string organization { get; set; } = string.Empty;
    public decimal amount { get; set; }
    public string reg_number { get; set; } = string.Empty;
    public string interest { get; set; } = string.Empty;
    public string accreditation { get; set; } = string.Empty;
    public string country { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public string date_of_onboarding { get; set; } = string.Empty;
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
    public string name { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string mobile { get; set; } = string.Empty;
    public string organization { get; set; } = string.Empty;
    public decimal amount { get; set; }
    public string reg_number { get; set; } = string.Empty;
    public string interest { get; set; } = string.Empty;
    public string accreditation { get; set; } = string.Empty;
    public string country { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public string date_of_onboarding { get; set; } = string.Empty;
    public string roi { get; set; } = string.Empty;
    public string roiType { get; set; } = string.Empty;
    public string bank { get; set; } = string.Empty;
    public string acNumber { get; set; } = string.Empty;
}

public class RegisterInvestorDTO
{
    public string name { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
    public string mobile { get; set; } = string.Empty;
    public string organization { get; set; } = string.Empty;
    public decimal amount { get; set; }
    public string reg_number { get; set; } = string.Empty;
    public string interest { get; set; } = string.Empty;
    public string accreditation { get; set; } = string.Empty;
    public string country { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public string date_of_onboarding { get; set; } = string.Empty;
    public string roi { get; set; } = string.Empty;
    public string roiType { get; set; } = string.Empty;
    public string bank { get; set; } = string.Empty;
    public string acNumber { get; set; } = string.Empty;
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