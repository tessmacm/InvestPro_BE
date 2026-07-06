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
        var investor = await _context.Investors
            .Include(i => i.Commitments)
            .Include(i => i.Documents)
            .FirstOrDefaultAsync(i => i.InvestorId == profileId);

        if (investor == null) return false;

        // Delete commitments and documents first to prevent FK constraint issues
        if (investor.Commitments != null && investor.Commitments.Any())
        {
            _context.InvestorCommitments.RemoveRange(investor.Commitments);
        }

        if (investor.Documents != null && investor.Documents.Any())
        {
            _context.InvestorDocuments.RemoveRange(investor.Documents);
        }

        var userId = investor.OwnerUserId;
        _context.Investors.Remove(investor);
        await _unitOfWork.CompleteAsync();

        if (!string.IsNullOrEmpty(userId))
        {
            var userAccount = await _userManager.FindByIdAsync(userId);
            if (userAccount != null)
            {
                await _userManager.DeleteAsync(userAccount);
            }
        }

        return true;
    }

    public async Task<IEnumerable<InvestorSummaryDTO>> GetAllInvestorsAsync()
    {
        var query = from inv in _context.Investors
                    join user in _context.Users on inv.OwnerUserId equals user.Id into userGroup
                    from user in userGroup.DefaultIfEmpty()
                    select new
                    {
                        Investor = inv,
                        User = user,
                        InvestorTypeName = inv.InvestorTypeNav != null ? inv.InvestorTypeNav.Name : null
                    };

        var results = await query.ToListAsync();

        return results.Select(r => new InvestorSummaryDTO
        {
            id = r.Investor.InvestorId ?? 0,
            name = r.User != null ? $"{r.User.FirstName} {r.User.LastName}".Trim() : "Unknown",
            email = r.User?.Email ?? "",
            mobile = r.User?.PhoneNumber ?? "",
            type = r.InvestorTypeName ?? "Individual",
            organization = r.Investor.LegalBusinessName ?? "—",
            authSingerName = r.Investor.AuthorizedSignerName ?? "Accredited",
            amount = r.Investor.CapitalAmount ?? 0,
            reg_number = r.Investor.CompanyRegistrationNo ?? "—",
            accreditation = r.Investor.AuthorizedSignerName ?? "Accredited",
            status = (r.User?.IsActive ?? true) ? "active" : "inactive",
            date_of_onboarding = r.User != null ? r.User.CreatedAt.ToString("dd MMM yyyy") : "15 May 2024",
        }).ToList();
    }

    public async Task<InvestorDetailsDTO?> GetInvestorDetailsByIdAsync(int investorId)
    {
        var investor = await _context.Investors
            .Include(i => i.InvestorTypeNav)
            .Include(i => i.RoiTypeNav)
            .Include(i => i.MinRoiRangeNav)
            .Include(i => i.MaxRoiRangeNav)
            .FirstOrDefaultAsync(i => i.InvestorId == investorId);
        if (investor == null) return null;
        var user = await _userManager.FindByIdAsync(investor.OwnerUserId!);
        return new InvestorDetailsDTO
        {
            id = investor.InvestorId ?? 0,
            name = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Unknown",
            email = user?.Email ?? "",
            mobile = user?.PhoneNumber ?? "",
            type = (int)investor.InvestorTypeId!,
            organization = investor.LegalBusinessName ?? "—",
            authSingerName = investor.AuthorizedSignerName ?? "Accredited",
            amount = investor.CapitalAmount ?? 0,
            reg_number = investor.CompanyRegistrationNo ?? "—",
            accreditation = investor.AuthorizedSignerName ?? "Accredited",
            status = (user?.IsActive ?? true) ? "active" : "inactive",
            date_of_onboarding = investor.DateOfBoarding.HasValue 
                ? investor.DateOfBoarding.Value.ToString("dd MMM yyyy") 
                : "15 May 2024",
            min_roi_id = investor.MinRoiRangeId,
            max_roi_id = investor.MaxRoiRangeId,
            roiTypeId = investor.RoiTypeId,
            bank = investor.BankName,
            acNumber = investor.BankAccountNo,
            sortCode = investor.SortCode,
            notes = investor.Notes
        };
    }

    public async Task<InvestorRegistrationResponse> RegisterAndCreateInvestorAsync(RegisterInvestorDTO dto)
    {
        // Step 1: Initialize Identity User First
        var names = (dto.name ?? "").Split(' ');
        var firstName = names.FirstOrDefault() ?? "Investor";
        var lastName = names.Length > 1 ? string.Join(" ", names.Skip(1)) : "User";

        var identityUser = new ApplicationUser
        {
            UserName = dto.email,
            Email = dto.email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = dto.mobile,
            IsActive = dto.status != "inactive",
            EmailConfirmed = true
        };

        var pwd = string.IsNullOrEmpty(dto.password) ? "Password123!" : dto.password;
        var identityResult = await _userManager.CreateAsync(identityUser, pwd);

        if (!identityResult.Succeeded)
        {
            var error = identityResult.Errors.FirstOrDefault()?.Description ?? "Identity Creation failed";
            return new InvestorRegistrationResponse { IsSuccess = false, ErrorMessage = error };
        }

        await _userManager.AddToRoleAsync(identityUser, "investor");

        // Step 2: Create Investor entity
        var newInvestor = new Investor
        {
            OwnerUserId = identityUser.Id,
            DateOfBirth = DateTime.UtcNow.AddYears(-18),
            TaxIdOrSSN = "-",
            LegalBusinessName = dto.organization ?? "—",
            CompanyRegistrationNo = dto.reg_number ?? "—",
            AuthorizedSignerName = dto.accreditation ?? "Accredited",
            CapitalAmount = dto.amount,
            Notes = "Basic Investor Registraion",
            InvestorTypeId = dto.type,
            //InvestmentInterestId = int.TryParse(dto.interest, out var intId) ? intId : 1,
            DateOfBoarding = DateTime.TryParse(dto.date_of_onboarding, out var dob) ? dob : DateTime.UtcNow,
            MinRoiRangeId = dto.min_RoiRangeId ?? 1,
            MaxRoiRangeId = dto.max_RoiRangeId ?? 2,
            RoiTypeId = dto.roiTypeId ?? 3,
            BankName = dto.bank,
            BankAccountNo = dto.acNumber,
            SortCode = dto.soreCode
        };

        // Save via Unit of Work
        await _context.Investors.AddAsync(newInvestor);
        await _unitOfWork.CompleteAsync();

        // Auto-generate payment schedule
        await GeneratePaymentsForInvestorAsync(newInvestor);
        await _unitOfWork.CompleteAsync();

        // Step 3: Backward link the InvestorId reference
        identityUser.InvestorId = newInvestor.InvestorId;
        await _userManager.UpdateAsync(identityUser);

        // Step 4: Generate token
        var rawToken = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

        return new InvestorRegistrationResponse
        {
            IsSuccess = true,
            Email = identityUser.Email ?? string.Empty,
            FullName = $"{identityUser.FirstName} {identityUser.LastName}".Trim(),
            InvestorId = newInvestor.InvestorId ?? 0,
            UserId = identityUser.Id,
            VerificationToken = encodedToken
        };
    }

    public async Task<bool> UpdateInvestorDetailsAsync(int profileId, UpdateInvestorDetailsDTO dto)
    {
        var investor = await _context.Investors
            .FirstOrDefaultAsync(i => i.InvestorId == profileId);

        if (investor == null) return false;

        investor.LegalBusinessName = dto.organization ?? "—";
        investor.CompanyRegistrationNo = dto.reg_number ?? "—";
        investor.CapitalAmount = dto.amount;
        investor.AuthorizedSignerName = dto.accreditation ?? "Accredited";
        investor.TaxIdOrSSN = "—";
        investor.Notes = dto.notes ?? "Basic";
        investor.InvestorTypeId = dto.type;
        //investor.InvestmentInterestId = int.TryParse(dto.interest, out var intId) ? intId : 1;
        investor.DateOfBoarding = DateTime.TryParse(dto.date_of_onboarding, out var dob) ? dob : DateTime.UtcNow;
        investor.MinRoiRangeId = dto.min_roi_id;
        investor.MaxRoiRangeId = dto.max_roi_id;
        investor.RoiTypeId = dto.roiTypeId ?? 3;
        investor.BankName = dto.bank;
        investor.BankAccountNo = dto.acNumber;
        investor.SortCode = dto.sortCode;

        // Recalculate pending payments
        var pendingPayments = await _context.Payments
            .Where(p => p.InvestorId == profileId && !p.IsSent && !p.IsReceived)
            .ToListAsync();
        if (pendingPayments.Any())
        {
            _context.Payments.RemoveRange(pendingPayments);
        }
        await GeneratePaymentsForInvestorAsync(investor);

        if (!string.IsNullOrEmpty(investor.OwnerUserId))
        {
            var user = await _userManager.FindByIdAsync(investor.OwnerUserId);
            if (user != null)
            {
                var names = (dto.name ?? "").Split(' ');
                user.FirstName = names.FirstOrDefault() ?? "Investor";
                user.LastName = names.Length > 1 ? string.Join(" ", names.Skip(1)) : "User";
                user.PhoneNumber = dto.mobile;
                user.IsActive = dto.status != "inactive";
                
                // Update Email if it changed and does not clash
                if (user.Email != dto.email)
                {
                    user.Email = dto.email;
                    user.UserName = dto.email;
                    user.NormalizedEmail = _userManager.NormalizeEmail(dto.email);
                    user.NormalizedUserName = _userManager.NormalizeName(dto.email);
                }

                await _userManager.UpdateAsync(user);
            }
        }

        return await _unitOfWork.CompleteAsync() >= 0;
    }

    private async Task GeneratePaymentsForInvestorAsync(Investor investor)
    {
        decimal percentage = 0.05m;
        var roiRange = await _context.RoiRanges.FindAsync(investor.MinRoiRangeId ?? 1);
        if (roiRange != null)
        {
            percentage = roiRange.Percentage;
        }

        int numPayments = 12;
        int monthsInterval = 1;
        decimal divisor = 12m;

        if (investor.RoiTypeId == 1) // Fixed
        {
            numPayments = 1;
            monthsInterval = 0;
            divisor = 1m;
        }
        else if (investor.RoiTypeId == 2) // Weekly
        {
            numPayments = 52;
            monthsInterval = 0;
            divisor = 52m;
        }
        else if (investor.RoiTypeId == 4) // Quarterly
        {
            numPayments = 4;
            monthsInterval = 3;
            divisor = 4m;
        }
        else if (investor.RoiTypeId == 5) // Yearly
        {
            numPayments = 1;
            monthsInterval = 12;
            divisor = 1m;
        }
        else // Monthly (Default)
        {
            numPayments = 12;
            monthsInterval = 1;
            divisor = 12m;
        }

        decimal capital = investor.CapitalAmount ?? 0m;
        decimal paymentAmount = Math.Round((capital * percentage) / divisor, 2);

        var onboardingDate = investor.DateOfBoarding ?? DateTime.UtcNow;

        for (int i = 1; i <= numPayments; i++)
        {
            DateTime paymentDate;
            if (investor.RoiTypeId == 2) // Weekly
            {
                paymentDate = onboardingDate.AddDays(i * 7);
            }
            else if (investor.RoiTypeId == 1) // Fixed
            {
                paymentDate = onboardingDate;
            }
            else
            {
                paymentDate = onboardingDate.AddMonths(i * monthsInterval);
            }

            var payment = new Payment
            {
                InvestorId = investor.InvestorId ?? 0,
                Amount = paymentAmount,
                PaymentDate = paymentDate,
                Status = "Pending",
                IsSent = false,
                IsReceived = false
            };
            await _context.Payments.AddAsync(payment);
        }
    }
}
