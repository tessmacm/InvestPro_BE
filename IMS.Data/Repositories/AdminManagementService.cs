using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Persistance.Repositories;

public class AdminManagementService : IAdminManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public AdminManagementService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IEnumerable<AdminUserSummaryDTO>> GetAllAdminUsersAsync()
    {
        var query = from user in _context.Users
                    join userRole in _context.UserRoles on user.Id equals userRole.UserId
                    join role in _context.Roles on userRole.RoleId equals role.Id
                    where role.Name == "admin" || role.Name == "superadmin" || role.Name == "manager" || role.Name == "client" || role.Name == "investor"
                    select new AdminUserSummaryDTO
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        IsActive = user.IsActive,
                        Role = role.Name!
                    };

        var list = await query.ToListAsync();
        return list.GroupBy(u => u.Id).Select(g => g.First()).ToList();
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
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(adminUser, createDto.Password);

        if (result.Succeeded)
        {
            var roleName = string.IsNullOrWhiteSpace(createDto.Role) ? "admin" : createDto.Role.ToLowerInvariant();
            var roleResult = await _userManager.AddToRoleAsync(adminUser, roleName);
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

    private async Task<bool> IsLastAdminAsync(string userId)
    {
        var admins = await _userManager.GetUsersInRoleAsync("admin");
        var superadmins = await _userManager.GetUsersInRoleAsync("superadmin");
        var allAdmins = admins.Concat(superadmins).Where(u => u.IsActive).GroupBy(u => u.Id).Select(g => g.First()).ToList();
        
        return allAdmins.Any(a => a.Id == userId) && allAdmins.Count <= 1;
    }

    public async Task<bool> UpdateAdminUserStatusAsync(string Id, UpdateAdminUserStatusDTO statusDto)
    {
        var adminUser = await _userManager.FindByIdAsync(Id);
        if (adminUser == null)
        {
            return false;
        }

        // If deactivating, verify it's not the last admin
        if (!statusDto.Status && await IsLastAdminAsync(Id))
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

        var newRole = roleDto.Role.ToLowerInvariant();
        var isNewRoleAdmin = newRole == "admin" || newRole == "superadmin";

        // If changing from admin to non-admin, verify it's not the last admin
        if (!isNewRoleAdmin && await IsLastAdminAsync(Id))
        {
            return false;
        }

        var currentRoles = await _userManager.GetRolesAsync(adminUser);
        var removeResult = await _userManager.RemoveFromRolesAsync(adminUser, currentRoles);
        if (!removeResult.Succeeded)
        {
            return false;
        }
        var addResult = await _userManager.AddToRoleAsync(adminUser, newRole);
        return addResult.Succeeded;
    }

    public async Task<bool?> DeleteAdminUserAsync(string Id)
    {
        var adminUser = await _userManager.FindByIdAsync(Id);
        if (adminUser == null)
        {
            return false;
        }

        // Verify it's not the last admin
        if (await IsLastAdminAsync(Id))
        {
            return false;
        }

        var result = await _userManager.DeleteAsync(adminUser);
        return result.Succeeded;
    }
}
