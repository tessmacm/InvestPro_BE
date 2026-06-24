using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Persistance.Repositories;

public class AdminManagementService : IAdminManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminManagementService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IEnumerable<AdminUserSummaryDTO>> GetAllAdminUsersAsync()
    {
        var admins = await _userManager.GetUsersInRoleAsync("admin");
        var superAdmins = await _userManager.GetUsersInRoleAsync("superadmin");
        
        var rawAdmins = admins.Concat(superAdmins).GroupBy(u => u.Id).Select(g => g.First()).ToList();

        return rawAdmins.Select(a => new AdminUserSummaryDTO
        {
            Id = a.Id,
            Email = a.Email!,
            FirstName = a.FirstName,
            LastName = a.LastName,
            IsActive = a.IsActive,
        }).ToList();
    }
    public async Task<(bool Succeeded, string[] Errors)> CreateAdminUserAsync(CreateAdminUserDTO createDto)
    {
        var adminUser = new ApplicationUser
        {
            UserName = createDto.Email,
            Email = createDto.Email,
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            InvestorId = null,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(adminUser, createDto.Password);

        if (result.Succeeded)
        {
            var roleResult = await _userManager.AddToRoleAsync(adminUser, "admin");
            if (roleResult.Succeeded)
            {
                return (true, Array.Empty<string>());
            }
            return (false, roleResult.Errors.Select(e => e.Description).ToArray());
        }
        return (false, result.Errors.Select(e => e.Description).ToArray());
    }
    public async Task<bool> UpdateAdminUserDetailsAsync(string userId, UpdateAdminUserDTO updateDto)
    {
       var adminUser = await _userManager.FindByIdAsync(userId);

        if (adminUser == null)
        {
            return false;
        }

        var userRole = await _userManager.GetRolesAsync(adminUser);
       
        // Updating existing Admin User
        adminUser.Email = updateDto.Email;
        adminUser.UserName = updateDto.Email;
        adminUser.FirstName = updateDto.Name.Split(' ')[0];
        adminUser.LastName = updateDto.Name.Split(' ').Length > 1 ? updateDto.Name.Split(' ')[1] : "";
        adminUser.IsActive = updateDto.Status;

        var result = await _userManager.UpdateAsync(adminUser);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(adminUser, updateDto.Role.ToLowerInvariant());
            return true;
        }
        return false;
    }

    public async Task<bool> UpdateAdminUserStatusAsync(string Id, UpdateAdminUserStatusDTO statusDto)
    {
        var adminUser = await _userManager.FindByIdAsync(Id);
        if (adminUser == null)
        {
            return false;
        }
        adminUser.IsActive = statusDto.Status;
        var result = await _userManager.UpdateAsync(adminUser);
        return result.Succeeded;
    }

    public async Task<bool> UpdateAdminUserRoleAsync(string Id, UpdateAdminUserRoleDTO roleDto)
    {
        var adminUser = await _userManager.FindByIdAsync(Id);
        if (adminUser == null)
        {
            return false;
        }
        var currentRoles = await _userManager.GetRolesAsync(adminUser);
        var removeResult = await _userManager.RemoveFromRolesAsync(adminUser, currentRoles);
        if (!removeResult.Succeeded)
        {
            return false;
        }
        var addResult = await _userManager.AddToRoleAsync(adminUser, roleDto.Role.ToLowerInvariant());
        return addResult.Succeeded;
    }

    public async Task<bool?> DeleteAdminUserAsync(string Id)
    {
        var adminUser = await _userManager.FindByIdAsync(Id);
        if (adminUser == null)
        {
            return false;
        }
        var result = await _userManager.DeleteAsync(adminUser);
        return result.Succeeded;
    }
}
