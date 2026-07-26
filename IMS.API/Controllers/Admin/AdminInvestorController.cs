using IMS.API.Services.EmailService;
using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IMS.API.Controllers;

namespace IMS.API.Controllers.Admin
{
    [Route("api/admin/investors")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Policy = "ElevatedOrManager")]
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
            // Fetch all Active Investors using the service
            var investors = await _investorService.GetAllInvestorsAsync();
            return Ok(investors);
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
                    await _emailService.SendEmailAsync(
                        response.Email,
                        "Welcome to InvestPro",
                        $"Welcome to InvestPro! You have been added as an Investor. You can now log in using your registered email address.\n\nYour login verification code is: {otp}. This code will expire in 10 minutes.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EmailService WARNING] Could not send welcome email to {response.Email}: {ex.Message}");
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
            if (result)
                return Ok(new { Message = "Investor details updated successfully." });
            return BadRequest(new { Message = "Failed to update investor details." });
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
