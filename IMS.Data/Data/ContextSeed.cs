using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using IMS.Core.Entities;

namespace IMS.Persistance.Data;

public class ContextSeed
{
    public static async Task SeedRolesAndAdminAdync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext? context = null)
    {
        // 1. Seed Roles into AspNetRoles table
        string[] roleNames = new string[] { "admin", "manager", "investor", "client" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. Ensure tessma.cm@gmail.com exists as Admin
        var adminUser = await userManager.FindByEmailAsync("tessma.cm@gmail.com");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "tessma.cm@gmail.com",
                Email = "tessma.cm@gmail.com",
                FirstName = "Tessma",
                LastName = "Admin",
                EmailConfirmed = true,
                IsActive = true
            };
            await userManager.CreateAsync(adminUser, "Password123!");
            await userManager.AddToRoleAsync(adminUser, "admin");
        }
        else
        {
            if (!await userManager.IsInRoleAsync(adminUser, "admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "admin");
            }
        }

        // 3. Ensure default project "Current Operations" exists
        if (context != null)
        {
            var currentOps = context.Projects.FirstOrDefault(p => p.Title == "Current Operations");
            if (currentOps == null)
            {
                currentOps = new Project
                {
                    Title = "Current Operations",
                    Description = "Core investment portfolio management & active operations",
                    TargetFunding = 1000000.00m,
                    FundedAmount = 0.00m,
                    LaunchDate = DateTime.UtcNow,
                    Status = "Active"
                };
                context.Projects.Add(currentOps);
                await context.SaveChangesAsync();
            }
        }
    }
}
