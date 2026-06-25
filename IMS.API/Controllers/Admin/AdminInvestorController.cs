using IMS.API.Services.EmailService;
using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
            var investors = await _investorService.GetAllInvestorsAsync();
            if (User.IsInRole("investor"))
            {
                var user = await _userManager.GetUserAsync(User);
                var investorId = user?.InvestorId ?? 0;
                investors = investors.Where(i => i.id == investorId).ToList();
            }
            return Ok(investors);
        }


        // POST: api/admin/investors/create
        [HttpPost("create")]
        [Authorize(Policy = "ElevatedOrManager")]
        public async Task<IActionResult> AdminCreateInvestorProfile([FromBody] RegisterInvestorDTO regCreateDto)
        {
            var response = await _investorService.RegisterAndCreateInvestorAsync(regCreateDto);

            if (!response.IsSuccess)
            {
                return BadRequest(new { Message = "Failed to create investor profile." });
            }
            var frontendUrl = "http://localhost:5078/api/Auth/"; // Replace with your actual frontend URL

            var confirmationLink = $"{frontendUrl}/register-verify?userId={response.UserId}&token={response.VerificationToken}";

            await _emailService.SendEmailAsync(
                response.Email!,
                "Email Confirmation",
                $"Please confirm your email by clicking on the link: {confirmationLink}");

            return Ok(new
            {
                Message = "Investor profile created successfully."
            });
        }

        [HttpPut("update/{Id}")]
        [Authorize(Policy = "ElevatedOrManager")]
        public async Task<IActionResult> UpdateInvestorDetails(int Id, [FromBody] UpdateInvestorDetailsDTO updateDto)
        {
            var result = await _investorService.UpdateInvestorDetailsAsync(Id, updateDto);
            if (result)
                return Ok(new { Message = "Investor details updated successfully." });
            return BadRequest(new { Message = "Failed to update investor details." });
        }

        [HttpDelete("{Id}")]
        [Authorize(Policy = "ElevatedOrManager")]
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

    }
}
