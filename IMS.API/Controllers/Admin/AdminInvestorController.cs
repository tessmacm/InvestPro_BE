using IMS.API.Services.EmailService;
using IMS.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IMS.API.Controllers.Admin
{
    [Route("api/admin/investors")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Super-admin")] // Only allow Admin role to access this controller
    public class AdminInvestorController : ControllerBase
    {
        private readonly IInvestorManagementService _investorService;
        private readonly IEmailService _emailService;

        public AdminInvestorController(IInvestorManagementService investorService,
            IEmailService emailService)
        {
            _investorService = investorService;
            _emailService = emailService;
        }

        // GET: api/admin/investors
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ElevatedRights")] // Only allow Admin role to access this controller
        public async Task<IActionResult> GetAllInvestors()
        {
            var investors = await _investorService.GetAllInvestorsAsync();
            return Ok(investors);
        }


        // POST: api/admin/investors/create
        [HttpPost("create")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ElevatedRights")] // Only allow Admin role to access this controller
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ElevatedRights")] // Only allow Admin role to access this controller
        public async Task<IActionResult> UpdateInvestorDetails(int Id, [FromBody] UpdateInvestorDetailsDTO updateDto)
        {
            var result = await _investorService.UpdateInvestorDetailsAsync(Id, updateDto);
            if (result)
                return Ok(new { Message = "Investor details updated successfully." });
            return BadRequest(new { Message = "Failed to update investor details." });
        }

        [HttpDelete("{Id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ElevatedRights")] // Only allow Admin role to access this controller
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
