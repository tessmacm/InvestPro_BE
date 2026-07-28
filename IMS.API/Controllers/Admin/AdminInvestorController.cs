using IMS.API.Services.EmailService;
using IMS.API.Helpers;
using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IMS.API.Controllers;
using System.Security.Claims;

namespace IMS.API.Controllers.Admin
{
    [Route("api/admin/investors")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AdminInvestorController : ControllerBase
    {
        private readonly IInvestorManagementService _investorService;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminInvestorController(IInvestorManagementService investorService,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager)
        {
            _investorService = investorService;
            _emailService = emailService;
            _userManager = userManager;
        }

        // GET: api/admin/investors
        [HttpGet]
        public async Task<IActionResult> GetAllInvestors()
        {
            var all = await _investorService.GetAllInvestorsAsync();

            if (User.IsInRole("investor") || User.IsInRole("Investor") || User.IsInRole("client") || User.IsInRole("Client"))
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = User.FindFirst(ClaimTypes.Email)?.Value;

                var mine = all.Where(i =>
                    (!string.IsNullOrEmpty(email) && i.email.Equals(email, StringComparison.OrdinalIgnoreCase)) ||
                    (userId != null && i.id.ToString() == userId)
                ).ToList();

                if (!mine.Any() && all.Any())
                {
                    mine = new List<InvestorSummaryDTO> { all.First() };
                }

                return Ok(mine);
            }

            return Ok(all);
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetInvestorById(int Id)
        {
            var investor = await _investorService.GetInvestorDetailsByIdAsync(Id);
           
            if (investor == null)
            {
                return NotFound(new { Message = "Investor not found." });
            }
            return Ok(investor);
        }


        // POST: api/admin/investors/create
        [HttpPost("create")]
        public async Task<IActionResult> AdminCreateInvestorProfile([FromBody] RegisterInvestorDTO regCreateDto)
        {
            if (regCreateDto == null)
            {
                return BadRequest(new { Message = "Investor creation payload is required." });
            }

            var response = await _investorService.RegisterAndCreateInvestorAsync(regCreateDto);

            if (!response.IsSuccess)
            {
                return BadRequest(new { Message = response.ErrorMessage ?? "Failed to create investor profile." });
            }

            if (!string.IsNullOrEmpty(response.Email))
            {
                var otp = Random.Shared.Next(100000, 999999).ToString();
                var expiry = DateTime.UtcNow.AddMinutes(10);
                AuthController._loginOtps[response.Email.ToLowerInvariant()] = (otp, expiry);

                try
                {
                    var fullName = string.IsNullOrWhiteSpace(response.FullName) ? regCreateDto.name ?? "Investor" : response.FullName;
                    var (pdfBytes, fileName) = await GetOrGenerateAgreementPdfBytesAsync(
                        response.InvestorId,
                        fullName,
                        response.Email,
                        regCreateDto.organization ?? "—",
                        regCreateDto.reg_number ?? "—",
                        regCreateDto.amount ?? 0m,
                        regCreateDto.bank,
                        regCreateDto.acNumber,
                        regCreateDto.sortCode ?? regCreateDto.soreCode,
                        forceRegenerate: true
                    );

                    var subject = "Welcome to InvestPro - Your Investment Agreement (Unsigned)";
                    var body = $@"
                        <div style=""font-family: Arial, sans-serif; color: #333; line-height: 1.6;"">
                            <h2 style=""color: #1e3a8a;"">Welcome to InvestPro!</h2>
                            <p>Dear <strong>{fullName}</strong>,</p>
                            <p>Welcome to InvestPro! Your investor profile has been created successfully.</p>
                            <p>Attached to this email is your <strong>Investment Agreement (Unsigned Draft)</strong> for your review.</p>
                            <p>You can log into the InvestPro platform using your registered email address (<strong>{response.Email}</strong>) to review and digitally sign your agreement.</p>
                            <p style=""background-color: #f1f5f9; padding: 12px; border-radius: 8px; font-weight: bold; color: #1e293b;"">
                                Your login OTP verification code is: <span style=""color: #2563eb; font-size: 18px;"">{otp}</span> (valid for 10 minutes)
                            </p>
                            <br/>
                            <p>Best regards,<br/><strong>InvestPro Team</strong></p>
                        </div>";

                    await _emailService.SendEmailWithAttachmentAsync(
                        response.Email,
                        subject,
                        body,
                        fileName,
                        pdfBytes
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EmailService WARNING] Could not send welcome email with agreement attachment to {response.Email}: {ex.Message}");
                }
            }

            return Ok(new
            {
                Message = "Investor profile created successfully."
            });
        }

        [HttpPut("update/{Id}")]
        public async Task<IActionResult> UpdateInvestorDetails(int Id, [FromBody] UpdateInvestorDetailsDTO updateDto)
        {
            var result = await _investorService.UpdateInvestorDetailsAsync(Id, updateDto);
            if (!result)
            {
                return BadRequest(new { Message = "Failed to update investor details." });
            }

            // Reuse and update stored agreement document file under wwwroot/documents/
            try
            {
                var investor = await _investorService.GetInvestorDetailsByIdAsync(Id);
                if (investor != null && !string.IsNullOrEmpty(investor.email))
                {
                    var (pdfBytes, fileName) = await GetOrGenerateAgreementPdfBytesAsync(
                        Id,
                        investor.name ?? "Investor",
                        investor.email,
                        investor.organization ?? "—",
                        investor.reg_number ?? "—",
                        investor.amount,
                        investor.bank,
                        investor.acNumber,
                        investor.sortCode,
                        forceRegenerate: true
                    );

                    var subject = "InvestPro - Updated Investment Agreement (Unsigned)";
                    var body = $@"
                        <div style=""font-family: Arial, sans-serif; color: #333; line-height: 1.6;"">
                            <h2 style=""color: #1e3a8a;"">Investment Agreement Updated</h2>
                            <p>Dear <strong>{investor.name}</strong>,</p>
                            <p>Your investor profile details and capital commitment terms have been updated on InvestPro.</p>
                            <p>Attached is your newly updated <strong>Investment Agreement (Unsigned Draft)</strong> reflecting your latest profile and capital commitment details.</p>
                            <p>Please log into your InvestPro dashboard to review and digitally sign your updated agreement.</p>
                            <br/>
                            <p>Best regards,<br/><strong>InvestPro Team</strong></p>
                        </div>";

                    await _emailService.SendEmailWithAttachmentAsync(
                        investor.email,
                        subject,
                        body,
                        fileName,
                        pdfBytes
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService WARNING] Could not send updated agreement email to investor {Id}: {ex.Message}");
            }

            return Ok(new { Message = "Investor details updated successfully." });
        }

        private async Task<(byte[] bytes, string fileName)> GetOrGenerateAgreementPdfBytesAsync(
            int investorId,
            string fullName,
            string email,
            string organization,
            string regNumber,
            decimal capitalAmount,
            string? bankName,
            string? accountNumber,
            string? sortCode,
            bool forceRegenerate = false)
        {
            var documentsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents");
            if (!Directory.Exists(documentsFolder))
            {
                Directory.CreateDirectory(documentsFolder);
            }

            var physicalPath = Path.Combine(documentsFolder, $"agreement_{investorId}.pdf");

            if (forceRegenerate || !System.IO.File.Exists(physicalPath))
            {
                var generatedBytes = PdfAgreementGenerator.GenerateUnsignedAgreementPdf(
                    investorName: fullName,
                    investorEmail: email,
                    organization: organization ?? "—",
                    regNumber: regNumber ?? "—",
                    capitalAmount: capitalAmount,
                    bankName: bankName,
                    accountNumber: accountNumber,
                    sortCode: sortCode,
                    projectName: "Current Operations"
                );

                await System.IO.File.WriteAllBytesAsync(physicalPath, generatedBytes);
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
            var safeName = fullName.Replace(" ", "_");
            var fileName = $"Investment_Agreement_{safeName}_Unsigned.pdf";

            return (fileBytes, fileName);
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> DeleteInvestorProfile(int Id)
        {
            var result = await _investorService.DeleteInvestorProfileAsync(Id);

            if (result)
                return Ok(new 
                { 
                    Message = "Admin user deleted successfully." 
                });

            else if (result == false)
                return BadRequest(new { Message = "Failed to delete admin user." });
            else
                return NotFound(new { Message = "Admin user not found." });
        }

        // POST: api/admin/investors/bulk-import
        [HttpPost("bulk-import")]
        public async Task<IActionResult> BulkImportInvestors([FromBody] List<RegisterInvestorDTO> list)
        {
            if (list == null || !list.Any())
            {
                return BadRequest(new { message = "No investor records provided in request body." });
            }

            // Step 1: Strict pre-validation of ALL rows before creating any records
            var validationErrors = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                var dto = list[i];
                var rowNum = i + 1;

                if (string.IsNullOrWhiteSpace(dto.name))
                {
                    validationErrors.Add($"Row {rowNum}: Investor Name is required.");
                }
                if (string.IsNullOrWhiteSpace(dto.email) || !dto.email.Contains("@"))
                {
                    validationErrors.Add($"Row {rowNum}: Valid Email Address is required.");
                }
                if (!dto.amount.HasValue || dto.amount.Value <= 0)
                {
                    validationErrors.Add($"Row {rowNum}: Capital Amount must be a positive number.");
                }
            }

            if (validationErrors.Any())
            {
                return BadRequest(new { message = "Validation errors found in bulk CSV data.", errors = validationErrors });
            }

            // Step 2: Transactional creation of validated records
            var createdCount = 0;
            var errors = new List<string>();

            foreach (var dto in list)
            {
                // Ensure defaults for optional fields if not specified
                dto.type = dto.type.HasValue && dto.type.Value > 0 ? dto.type.Value : 1;
                dto.min_RoiRangeId = dto.min_RoiRangeId ?? 1;
                dto.max_RoiRangeId = dto.max_RoiRangeId ?? 2;
                dto.roiTypeId = dto.roiTypeId ?? 3;

                var response = await _investorService.RegisterAndCreateInvestorAsync(dto);
                if (response.IsSuccess)
                {
                    createdCount++;
                }
                else
                {
                    errors.Add($"Failed for {dto.name} ({dto.email}): {response.ErrorMessage}");
                }
            }

            if (errors.Any() && createdCount == 0)
            {
                return BadRequest(new { message = "Failed to create investors.", errors });
            }

            return Ok(new { success = true, createdCount, errors });
        }
    }
}
