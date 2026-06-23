using IMS.Core.Entities;
using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace IMS.Persistance.Repositories;

public class InvestorManagementService : IInvestorManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public InvestorManagementService(UserManager<ApplicationUser> userManager, 
        IUnitOfWork unitOfWork,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public Task<bool> CreateInvestorProfileAsync(CreateInvestorProfileDTO dto)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> DeleteInvestorProfileAsync(int profileId)
    {
        //// 1. Fetch the investor profile record along with any tracking collections if needed
        //var investor = await _context.Investors
        //    .Include(i => i.Commitments)
        //    .FirstOrDefaultAsync(i => i.InvestorId == profileId);

        //if (investor == null) return false;

        //// 2. FINANCIAL GUARDRAIL: Check for historical data footprints

        //if (investor.Commitments!=null && investor.Commitments.Any())
        //{
        //    // OPTION A: SOFT DELETE / FREEZE (Highly Recommended for Auditing)
        //    // If they have investments, we do not delete them. We block access instead.

        //    if (!string.IsNullOrEmpty(investor.OwnerUserId))
        //    {
        //        var userAccount = await _userManager.FindByIdAsync(investor.OwnerUserId);

        //        if (userAccount!=null)
        //        {
        //            userAccount.IsActive = false; // Freeze system login capability
        //            await _userManager.UpdateAsync(userAccount);
        //        }
        //    }
        //}

        return true;
    }

    public async Task<IEnumerable<InvestorSummaryDTO>> GetAllInvestorsAsync()
    {
        return await _userManager.Users
            .Where(u => u.InvestorId != null) // Filter to only users linked to investors
            .Select(u => new InvestorSummaryDTO
            {
                OwnerUserId = u.Id,
                Email = u.Email!,
                FullName = $"{u.FirstName} {u.LastName}".Trim(),
                InvestorId = u.InvestorId ?? 0, // Safe fallback, though should never be null here
                IsActive = u.IsActive, // Assuming all users with an investor profile are active; adjust as needed
                InvestorTypeName = u.InvestorNav!.InvestorTypeNav!.Name, // Custom method to fetch type name
                InvestmentRange = u.InvestorNav.InvestmentInterestNav!.DisplayRange // Custom method to fetch range
            })
            .ToListAsync();
    }

    public async Task<InvestorRegistrationResponse> RegisterAndCreateInvestorAsync(RegisterInvestorDTO dto)
    {
        // Step 1: Initialize Identity User First

        var identityUser = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FullName.Split(' ').FirstOrDefault() ?? dto.FullName,
            LastName = dto.FullName.Split(' ').LastOrDefault() ?? "Investor",
        };

        var identityResult = await _userManager.CreateAsync(identityUser, dto.Password);

        if (!identityResult.Succeeded)
        {
            var error = identityResult.Errors.FirstOrDefault()!.Description ?? "Idenity Creation is failed";
            return new InvestorRegistrationResponse { IsSuccess = false, ErrorMessage = error};
;        }

        await _userManager.AddToRoleAsync(identityUser, "Investor");

        // Step 2: Create a New Domain Investor row safely with the authenticated cross-layer link Id
        var newInvestor = new Investor
        {
            OwnerUserId = identityUser.Id,
            DateOfBirth = DateTime.UtcNow.AddYears(-18),
            TaxIdOrSSN = "PENDING_KYC",   // Flag to indicate details are missing

            LegalBusinessName = "Business",
            CompanyRegistrationNo = "UK123",
            AuthorizedSignerName = "Authorized",
            CapitalAmount = 0,
            Notes = "Public Web Registration Profile Stub",
            InvestorTypeId = 1,          // Defaulting strictly to "Individual"
            InvestmentInterestId = 1,    // Defaulting to "Unspecified / Under Evaluation"
        };

        // Save via Repository Pattern Unit of Work
        var investor = _unitOfWork.Investors.AddAsync(newInvestor);

        if (!investor.IsCompletedSuccessfully)
        {
            await _userManager.DeleteAsync(identityUser);
            await _userManager.RemoveFromRoleAsync(identityUser,"Investor");
            return new InvestorRegistrationResponse { IsSuccess = false, ErrorMessage = "Investor creation failsed." };

        }
        await _unitOfWork.CompleteAsync(); 

        // Step 3: Backward link the InvestorId reference back to Identity to sync your data contexts
        identityUser.InvestorId = newInvestor.InvestorId;
        await _userManager.UpdateAsync(identityUser);

        // Step 4: Generate a verification token (for email confirmation, etc.)
        var rawToken = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

        // Step 6: Return the registration response with all relevant details
        return new InvestorRegistrationResponse
        {
            IsSuccess = true,
            Email = identityUser.Email! ?? string.Empty,
            FullName = $"{identityUser.FirstName} {identityUser.LastName}".Trim(),
            InvestorId = (int)newInvestor.InvestorId!,
            UserId = identityUser.Id,
            VerificationToken = encodedToken
        };

    }

    public Task<bool> UpdateInvestorDetailsAsync(int profileId, UpdateInvestorDetailsDTO dto)
    {
        throw new NotImplementedException();
    }
}
