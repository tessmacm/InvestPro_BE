using IMS.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IMS.API.Controllers.Admin;

[Route("api/admin/users")]
[ApiController]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminManagementService _adminService;

    public AdminUsersController(IAdminManagementService adminService)
    {
        _adminService = adminService;
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
            return Ok(new { Message = "Admin user created successfully." });
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
        return BadRequest(new { Message = "Failed to update admin user status." });
    }

    [HttpPatch("{Id}/role")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "SuperAdminOnly")] // Only allow Admin role to access this controller
    public async Task<IActionResult> UpdateAdminUserRole(string Id, [FromBody] UpdateAdminUserRoleDTO roleDto)
    {
        var result = await _adminService.UpdateAdminUserRoleAsync(Id, roleDto);
        if (result)
            return Ok(new { Message = "Admin user role updated successfully." });
        return BadRequest(new { Message = "Failed to update admin user role." });
    }

    [HttpDelete("{Id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "SuperAdminOnly")] // Only allow Admin role to access this controller
    public IActionResult DeleteAdminUser(string Id)
    {
        var result = _adminService.DeleteAdminUserAsync(Id).Result;
        if (result == true)
            return Ok(new { Message = "Admin user deleted successfully." });
        else if (result == false)
            return BadRequest(new { Message = "Failed to delete admin user." });
        else
            return NotFound(new { Message = "Admin user not found." });
    }
}
