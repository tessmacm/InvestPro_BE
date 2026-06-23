using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Persistance.Data;

public class ContextSeed
{
    public static async Task SeedRolesAndAdminAdync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        #region Creating Roles
        // 1. Seed Roles into AspNetRoles table
        //string[] roleNames = new string[] { "SuperAdmin","Admin", "Investor" };

        //var roles = 
        //foreach (var roleName in roleNames)
        //{
        //    if (!await roleManager.RoleExistsAsync(roleName))
        //    {
        //        await roleManager.CreateAsync(new IdentityRole(roleName));
        //    }
        //}
        #endregion

        // 2. Seed Admin User into AspNetUsers table
        var defaultAdminEmail = "admin@investpro.com";
        var defaultAdmin = await userManager.FindByEmailAsync(defaultAdminEmail);

        if (defaultAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = defaultAdminEmail,
                Email = defaultAdminEmail,
                FirstName = "System",
                LastName = "Adminstrator",
                EmailConfirmed = true,
                InvestorId = null, // No investor profile for admin
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var createAdminResult = await userManager.CreateAsync(adminUser, "Admin@123");
            if (createAdminResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Super-Admin");
            }
        }
    }
}
