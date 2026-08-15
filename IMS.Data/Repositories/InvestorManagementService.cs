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

        // 1. Delete payments linked to investor
        var payments = await _context.Payments.Where(p => p.InvestorId == profileId).ToListAsync();
        if (payments.Any())
        {
            _context.Payments.RemoveRange(payments);
        }

        // 2. Delete ROI contracts linked to investor
        var roiContracts = await _context.RoiContracts.Where(r => r.InvestorId == profileId).ToListAsync();
        if (roiContracts.Any())
        {
            _context.RoiContracts.RemoveRange(roiContracts);
        }

        // 3. Delete commitments and documents
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

        // 4. Completely remove associated ApplicationUser from AspNetUsers
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
        var query = from inv in _context.Investors.AsNoTracking()
                    join user in _context.Users.AsNoTracking() on inv.OwnerUserId equals user.Id into userGroup
                    from user in userGroup.DefaultIfEmpty()
                    orderby inv.InvestorId descending
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
            name = r.User != null ? (r.User.LastName == "User" || string.IsNullOrWhiteSpace(r.User.LastName) ? r.User.FirstName : $"{r.User.FirstName} {r.User.LastName}".Trim()) : (r.Investor.LegalBusinessName ?? "Investor"),
            email = r.User?.Email ?? "",
            mobile = r.User?.PhoneNumber ?? "",
            type = r.InvestorTypeName ?? "Individual",
            organization = r.Investor.LegalBusinessName ?? "—",
            authSingerName = r.Investor.AuthorizedSignerName ?? "Accredited",
            amount = r.Investor.CapitalAmount ?? 0,
            reg_number = r.Investor.CompanyRegistrationNo ?? "—",
            status = (r.User?.IsActive ?? true) ? "active" : "inactive",
            date_of_onboarding = r.Investor.DateOfBoarding.HasValue ? r.Investor.DateOfBoarding.Value.ToString("yyyy-MM-dd") : (r.User != null ? r.User.CreatedAt.ToString("yyyy-MM-dd") : DateTime.UtcNow.ToString("yyyy-MM-dd")),
            min_roi_id = r.Investor.MinRoiRangeId,
            max_roi_id = r.Investor.MaxRoiRangeId,
            roiTypeId = r.Investor.RoiTypeId,
            payoutType = r.Investor.PayoutType ?? (r.Investor.RoiTypeId == 1 ? "Fixed" : "Variant"),
            bank = r.Investor.BankName ?? "—",
            acNumber = r.Investor.BankAccountNo ?? "—",
            sortCode = r.Investor.SortCode ?? "—",
            witness = string.IsNullOrWhiteSpace(r.Investor.Witness) ? null : r.Investor.Witness,
            address = string.IsNullOrWhiteSpace(r.Investor.Address) ? null : r.Investor.Address,
            projectId = r.Investor.ProjectId ?? 1,
            duration = r.Investor.Duration ?? "12 Months",
            notes = r.Investor.Notes ?? "—"
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
            name = user != null ? (user.LastName == "User" || string.IsNullOrWhiteSpace(user.LastName) ? user.FirstName : $"{user.FirstName} {user.LastName}".Trim()) : (investor.LegalBusinessName ?? "Investor"),
            email = user?.Email ?? "",
            mobile = user?.PhoneNumber ?? "",
            type = (int)(investor.InvestorTypeId ?? 1),
            organization = investor.LegalBusinessName ?? "—",
            authSingerName = investor.AuthorizedSignerName ?? "—",
            amount = investor.CapitalAmount ?? 0,
            reg_number = investor.CompanyRegistrationNo ?? "—",
            status = (user?.IsActive ?? true) ? "active" : "inactive",
            date_of_onboarding = investor.DateOfBoarding.HasValue 
                ? investor.DateOfBoarding.Value.ToString("yyyy-MM-dd") 
                : (user != null ? user.CreatedAt.ToString("yyyy-MM-dd") : DateTime.UtcNow.ToString("yyyy-MM-dd")),
            min_roi_id = investor.MinRoiRangeId,
            max_roi_id = investor.MaxRoiRangeId,
            roiTypeId = investor.RoiTypeId,
            payoutType = investor.PayoutType ?? (investor.RoiTypeId == 1 ? "Fixed" : "Variant"),
            bank = investor.BankName,
            acNumber = investor.BankAccountNo,
            sortCode = investor.SortCode,
            witness = investor.Witness,
            address = investor.Address,
            projectId = investor.ProjectId ?? 1,
            duration = investor.Duration ?? "12 Months",
            notes = investor.Notes
        };
    }

    public async Task<InvestorRegistrationResponse> RegisterAndCreateInvestorAsync(RegisterInvestorDTO dto)
    {
        var names = (dto.name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var firstName = names.FirstOrDefault() ?? "Investor";
        var lastName = names.Length > 1 ? string.Join(" ", names.Skip(1)) : "";

        // Step 1: Check if user already exists
        ApplicationUser identityUser;
        var existingUser = await _userManager.FindByEmailAsync(dto.email!);

        if (existingUser != null)
        {
            // Existing user account found -> Attach additional or new investment profile to this existing user!
            identityUser = existingUser;
            if (!string.IsNullOrEmpty(firstName) && firstName != "Investor")
            {
                identityUser.FirstName = firstName;
                identityUser.LastName = lastName;
            }
            identityUser.PhoneNumber = string.IsNullOrEmpty(dto.mobile) ? identityUser.PhoneNumber : dto.mobile;
            identityUser.IsActive = dto.status != "inactive";
            await _userManager.UpdateAsync(identityUser);
        }
        else
        {
            // Create brand new Identity user
            identityUser = new ApplicationUser
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
        }

        // Step 2: Create Investor entity (represents this specific investment contract)
        var newInvestor = new Investor
        {
            OwnerUserId = identityUser.Id,
            DateOfBirth = DateTime.UtcNow.AddYears(-18),
            TaxIdOrSSN = "-",
            LegalBusinessName = string.IsNullOrEmpty(dto.organization) ? "—" : dto.organization,
            CompanyRegistrationNo = string.IsNullOrEmpty(dto.reg_number) ? "—" : dto.reg_number,
            AuthorizedSignerName = "—",
            CapitalAmount = dto.amount ?? 0,
            Notes = string.IsNullOrEmpty(dto.notes) ? "Investor Registration" : dto.notes,
            InvestorTypeId = Math.Clamp(dto.type ?? 1, 1, 2),
            DateOfBoarding = DateTime.TryParse(dto.date_of_onboarding, out var dob) ? dob : DateTime.UtcNow,
            MinRoiRangeId = Math.Clamp(dto.min_RoiRangeId ?? dto.min_roi_id ?? 1, 1, 4),
            MaxRoiRangeId = Math.Clamp(dto.max_RoiRangeId ?? dto.max_roi_id ?? 4, 1, 4),
            RoiTypeId = Math.Clamp(dto.roiTypeId ?? 3, 1, 5),
            PayoutType = dto.payoutType ?? (dto.roiTypeId == 1 ? "Fixed" : "Variant"),
            BankName = dto.bank,
            BankAccountNo = dto.acNumber,
            SortCode = !string.IsNullOrEmpty(dto.sortCode) ? dto.sortCode : dto.soreCode,
            Witness = dto.witness,
            Address = dto.address,
            ProjectId = dto.projectId ?? 1,
            Duration = !string.IsNullOrEmpty(dto.duration) ? dto.duration : "12 Months",
            CreatedAt = DateTime.UtcNow
        };

        // Save via Unit of Work
        await _context.Investors.AddAsync(newInvestor);
        await _unitOfWork.CompleteAsync();

        // Auto-generate payment schedule for this investment
        await GeneratePaymentsForInvestorAsync(newInvestor);
        await _unitOfWork.CompleteAsync();

        // Project lookup for agreement title
        var proj = await _context.Projects.FindAsync(newInvestor.ProjectId ?? 1);
        var projectTitle = proj?.Title ?? "Current Operations";

        // Step 3b: Auto-attach Investment Agreement document for this specific investment
        var agreementDoc = new InvestorDocument
        {
            InvestorId = newInvestor.InvestorId ?? 0,
            Title = $"Investment Agreement - {identityUser.FirstName} {identityUser.LastName} ({projectTitle} - #{newInvestor.InvestorId}).pdf",
            DocumentType = "Agreement",
            Size = 1.2m,
            StorageUrl = $"/documents/agreement_{newInvestor.InvestorId}.pdf",
            UploadedAt = DateTime.UtcNow,
            UploadedById = identityUser.Id,
            Status = "Pending Signature"
        };
        await _context.InvestorDocuments.AddAsync(agreementDoc);
        await _unitOfWork.CompleteAsync();

        // Step 3: Link latest InvestorId reference on user
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
        investor.TaxIdOrSSN = "—";
        investor.Notes = dto.notes ?? "Basic";
        investor.InvestorTypeId = dto.type;
        investor.DateOfBoarding = DateTime.TryParse(dto.date_of_onboarding, out var dob) ? dob : DateTime.UtcNow;
        investor.MinRoiRangeId = dto.min_roi_id ?? dto.min_RoiRangeId;
        investor.MaxRoiRangeId = dto.max_roi_id ?? dto.max_RoiRangeId;
        investor.RoiTypeId = dto.roiTypeId ?? 3;
        investor.PayoutType = dto.payoutType;
        investor.BankName = dto.bank;
        investor.BankAccountNo = dto.acNumber;
        investor.SortCode = !string.IsNullOrEmpty(dto.sortCode) ? dto.sortCode : dto.soreCode;
        if (!string.IsNullOrEmpty(dto.witness)) investor.Witness = dto.witness;
        if (!string.IsNullOrEmpty(dto.address)) investor.Address = dto.address;
        if (dto.projectId.HasValue) investor.ProjectId = dto.projectId;
        if (!string.IsNullOrEmpty(dto.duration)) investor.Duration = dto.duration;

        // Recalculate pending payments for this investment
        var pendingPayments = await _context.Payments
            .Where(p => p.InvestorId == profileId && !p.IsSent && !p.IsReceived)
            .ToListAsync();
        if (pendingPayments.Any())
        {
            _context.Payments.RemoveRange(pendingPayments);
        }
        await GeneratePaymentsForInvestorAsync(investor);

        // Sync shared user and bank details across ALL investments belonging to this user
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
                if (!string.IsNullOrWhiteSpace(dto.email) && user.Email != dto.email)
                {
                    user.Email = dto.email;
                    user.UserName = dto.email;
                    user.NormalizedEmail = _userManager.NormalizeEmail(dto.email);
                    user.NormalizedUserName = _userManager.NormalizeName(dto.email);
                }

                await _userManager.UpdateAsync(user);
            }

            // Propagate common personal & bank fields to all other investment records under the same user
            var allUserInvestments = await _context.Investors
                .Where(i => i.OwnerUserId == investor.OwnerUserId && i.InvestorId != profileId)
                .ToListAsync();

            foreach (var otherInv in allUserInvestments)
            {
                if (!string.IsNullOrEmpty(dto.address)) otherInv.Address = dto.address;
                if (!string.IsNullOrEmpty(dto.witness)) otherInv.Witness = dto.witness;
                if (!string.IsNullOrEmpty(dto.bank)) otherInv.BankName = dto.bank;
                if (!string.IsNullOrEmpty(dto.acNumber)) otherInv.BankAccountNo = dto.acNumber;
                var sort = !string.IsNullOrEmpty(dto.sortCode) ? dto.sortCode : dto.soreCode;
                if (!string.IsNullOrEmpty(sort)) otherInv.SortCode = sort;
            }
        }

        // Reset Agreement Document to Pending Signature so investor can re-sign updated contract
        var agreementDoc = await _context.InvestorDocuments
            .FirstOrDefaultAsync(d => d.InvestorId == profileId && (d.DocumentType == "Agreement" || d.Title.Contains("Agreement")));

        if (agreementDoc != null)
        {
            var fullName = !string.IsNullOrWhiteSpace(dto.name) ? dto.name : "Investor";
            var proj = await _context.Projects.FindAsync(investor.ProjectId ?? 1);
            var projectTitle = proj?.Title ?? "Current Operations";
            agreementDoc.Title = $"Investment Agreement - {fullName} ({projectTitle} - #{investor.InvestorId}).pdf";
            agreementDoc.Status = "Pending Signature";
            agreementDoc.SignatureData = null;
            agreementDoc.SignedAt = null;
            agreementDoc.UploadedAt = DateTime.UtcNow;
        }

        investor.IsAgreedToTerms = false;

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
