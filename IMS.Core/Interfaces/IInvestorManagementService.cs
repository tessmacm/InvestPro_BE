using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static IMS.Core.Interfaces.UpdateInvestorDetailsDTO;

namespace IMS.Core.Interfaces;

public interface IInvestorManagementService
{
    Task<InvestorRegistrationResponse> RegisterAndCreateInvestorAsync(RegisterInvestorDTO dto);
    Task<IEnumerable<InvestorSummaryDTO>> GetAllInvestorsAsync();
    Task<InvestorDetailsDTO?> GetInvestorDetailsByIdAsync(int investorId);
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
    public string authSingerName { get; set; } = string.Empty;
    public decimal amount { get; set; }
    public string reg_number { get; set; } = string.Empty;
    public string accreditation { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public string date_of_onboarding { get; set; } = string.Empty;
    public int? min_roi_id { get; set; }
    public int? max_roi_id { get; set; }
    public int? roiTypeId { get; set; }
    public string? payoutType { get; set; }
    public string? bank { get; set; }
    public string? acNumber { get; set; }
    public string? sortCode { get; set; }
    public string? witness { get; set; }
    public string? address { get; set; }
    public int? projectId { get; set; }
    public string? notes { get; set; }
}

public class InvestorDetailsDTO
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public int type { get; set; }
    public string email { get; set; } = string.Empty;
    public string mobile { get; set; } = string.Empty;
    public string organization { get; set; } = string.Empty;
    public string authSingerName { get; set; } = string.Empty;
    public decimal amount { get; set; }
    public string reg_number { get; set; } = string.Empty;
    public string accreditation { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public string date_of_onboarding { get; set; } = string.Empty;
    public int? min_roi_id { get; set; }
    public int? max_roi_id { get; set; }
    public int? roiTypeId { get; set; }
    public string? payoutType { get; set; }
    public string? bank { get; set; }
    public string? acNumber { get; set; }
    public string? sortCode { get; set; }
    public string? witness { get; set; }
    public string? address { get; set; }
    public int? projectId { get; set; }
    public string? notes { get; set; }
}
public class CreateInvestorProfileDTO
{
    [Required, EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int InvestorTypeId { get; set; }
    public int InvestmentInterestId { get; set; }
}
public class UpdateInvestorDetailsDTO
{
    public string? name { get; set; }
    public int? type { get; set; }
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string? email { get; set; }
    public string? mobile { get; set; }
    public string? organization { get; set; }
    public string? authSingerName { get; set; }
    public decimal? amount { get; set; }
    public string? reg_number { get; set; }
    public string? accreditation { get; set; }
    public string? status { get; set; }
    public string? date_of_onboarding { get; set; }
    public int? min_roi_id { get; set; }
    public int? max_roi_id { get; set; }
    public int? min_RoiRangeId { get; set; }
    public int? max_RoiRangeId { get; set; }
    public int? roiTypeId { get; set; }
    public string? payoutType { get; set; }
    public string? bank { get; set; }
    public string? acNumber { get; set; }
    public string? sortCode { get; set; }
    public string? soreCode { get; set; }
    public string? witness { get; set; }
    public string? address { get; set; }
    public int? projectId { get; set; }
    public string? notes { get; set; }
}
public class RegisterInvestorDTO
{
    public string? name { get; set; }
    public int? type { get; set; } 
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string? email { get; set; } 
    public string? password { get; set; } 
    public string? mobile { get; set; } 
    public string? organization { get; set; }
    public string? authSingerName { get; set; }
    public decimal? amount { get; set; }
    public string? reg_number { get; set; } 
    public string? accreditation { get; set; }
    public string? status { get; set; } 
    public string? date_of_onboarding { get; set; } 
    public int? min_RoiRangeId { get; set; }
    public int? max_RoiRangeId { get; set; }
    public int? min_roi_id { get; set; }
    public int? max_roi_id { get; set; }
    public int? roiTypeId { get; set; }
    public string? payoutType { get; set; }
    public string? bank { get; set; }
    public string? acNumber { get; set; } 
    public string? soreCode { get; set; }
    public string? sortCode { get; set; }
    public string? witness { get; set; }
    public string? address { get; set; }
    public int? projectId { get; set; }
    public string? notes { get; set; } 
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