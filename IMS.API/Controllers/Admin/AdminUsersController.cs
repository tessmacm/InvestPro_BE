using IMS.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IMS.API.Services.EmailService;
using IMS.API.Controllers;
using System;

namespace IMS.API.Controllers.Admin;

[Route("api/admin/users")]
[ApiController]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminManagementService _adminService;
    private readonly IEmailService _emailService;

    public AdminUsersController(IAdminManagementService adminService, IEmailService emailService)
    {
        _adminService = adminService;
        _emailService = emailService;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Policy = "ElevatedRights")] // Only allow Admin role to access this controller
    public async Task<IActionResult> GetAllAdminUsers()
    {
        var admins = await _adminService.GetAllAdminUsersAsync();
        return Ok(admins);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "SuperAdminOnly")] // Only allow Admin role to access this controller
    public async Task<IActionResult> CreateAdminUser([FromBody] CreateAdminUserDTO createDto)
    {
        var (succeeded, errors) = await _adminService.CreateAdminUserAsync(createDto);
        if (succeeded)
        {
            var otp = Random.Shared.Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);
            AuthController._loginOtps[createDto.Email.ToLowerInvariant()] = (otp, expiry);

            await _emailService.SendEmailAsync(
                createDto.Email,
                "Welcome to InvestPro",
                $"Welcome to InvestPro! Your account has been created by the administrator with the role of '{createDto.Role}'. You can now log in using your registered email address.\n\nYour login verification code is: {otp}. This code will expire in 10 minutes.");

            return Ok(new { Message = "Admin user created successfully." });
        }
        return BadRequest(new { Message = "Failed to create admin user.", Errors = errors });
    }

    [HttpPut("{Id}/role")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "SuperAdminOnly")] // Only allow Admin role to access this controller
    public async Task<IActionResult> UpdateAdminUserDetails(string Id, [FromBody] UpdateAdminUserDTO updateDto)
    {
        var result = await _adminService.UpdateAdminUserDetailsAsync(Id, updateDto);
        if (result)
            return Ok(new { Message = "Admin user details updated successfully." });
        return BadRequest(new { Message = "Failed to update admin user details." });
    }

    [HttpPatch("{Id}/status")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "SuperAdminOnly")] // Only allow Admin role to access this controller
    public async Task<IActionResult> UpdateAdminUserStatus(string Id, [FromBody] UpdateAdminUserStatusDTO statusDto)
    {
        var result = await _adminService.UpdateAdminUserStatusAsync(Id, statusDto);
        if (result)
            return Ok(new { Message = "Admin user status updated successfully." });
        return BadRequest(new { Message = "Failed to update status. Cannot deactivate the only remaining active Administrator." });
    }

    [HttpPatch("{Id}/role")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "SuperAdminOnly")] // Only allow Admin role to access this controller
    public async Task<IActionResult> UpdateAdminUserRole(string Id, [FromBody] UpdateAdminUserRoleDTO roleDto)
    {
        var result = await _adminService.UpdateAdminUserRoleAsync(Id, roleDto);
        if (result)
            return Ok(new { Message = "Admin user role updated successfully." });
        return BadRequest(new { Message = "Failed to update role. Cannot demote the only remaining active Administrator." });
    }

    [HttpDelete("{Id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "SuperAdminOnly")] // Only allow Admin role to access this controller
    public IActionResult DeleteAdminUser(string Id)
    {
        var result = _adminService.DeleteAdminUserAsync(Id).Result;
        if (result == true)
            return Ok(new { Message = "Admin user deleted successfully." });
        else if (result == false)
            return BadRequest(new { Message = "Failed to delete admin user. Cannot delete the only remaining active Administrator." });
        else
            return NotFound(new { Message = "Admin user not found." });
    }
}
