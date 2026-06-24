using System;
using System.Collections.Generic;
using System.Text;
using IMS.Core.Entities;

namespace IMS.Core.Interfaces;

public interface IAdminManagementService
{
    // Returns a lightweight structural layout instead of the heavy Identity User object
    Task<IEnumerable<AdminUserSummaryDTO>> GetAllAdminUsersAsync();
    Task<(bool Succeeded, string[] Errors)> CreateAdminUserAsync(CreateAdminUserDTO createDto);
    Task<bool> UpdateAdminUserDetailsAsync(string userId, UpdateAdminUserDTO updateDto);
    Task<bool> UpdateAdminUserStatusAsync(string userId, UpdateAdminUserStatusDTO statusDto);
    Task<bool> UpdateAdminUserRoleAsync(string userId, UpdateAdminUserRoleDTO roleDto);
    Task<bool?> DeleteAdminUserAsync(string userId);
}

public class AdminUserSummaryDTO
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateAdminUserDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class UpdateAdminUserDTO
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // e.g., SuperAdmin, Admin, Moderator
    public bool Status { get; set; }
}

public class UpdateAdminUserRoleDTO
{
    public string Role { get; set; } = string.Empty; // e.g., SuperAdmin, Admin, Moderator
}

public class UpdateAdminUserStatusDTO
{
    public bool Status { get; set; }
}