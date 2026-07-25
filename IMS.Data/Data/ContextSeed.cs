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

        // 2. Ensure admin@investpro.com is removed if present and ensure first user has admin role
        var legacyAdmin = await userManager.FindByEmailAsync("admin@investpro.com");
        if (legacyAdmin != null)
        {
            await userManager.DeleteAsync(legacyAdmin);
        }

        // Auto-assign admin role to the first user if no admin exists
        var allUsersList = userManager.Users.ToList();
        var hasAdmin = false;
        foreach (var u in allUsersList)
        {
            var r = await userManager.GetRolesAsync(u);
            if (r.Contains("admin") || r.Contains("Admin") || r.Contains("superadmin") || r.Contains("SuperAdmin"))
            {
                hasAdmin = true;
                break;
            }
        }

        if (!hasAdmin && allUsersList.Any())
        {
            var firstUser = allUsersList.First();
            await userManager.AddToRoleAsync(firstUser, "admin");
        }

        // 3. WIPE ALL OPERATIONAL DATA EXCEPT USER ACCOUNTS (COMPLETED AS REQUESTED BY USER)
        if (context != null)
        {
            /*
            try
            {
                var usersWithInvestor = context.Users.Where(u => u.InvestorId != null).ToList();
                foreach (var u in usersWithInvestor)
                {
                    u.InvestorId = null;
                }
                await context.SaveChangesAsync();

                context.InvestorDocuments.RemoveRange(context.InvestorDocuments);
                context.Payments.RemoveRange(context.Payments);
                context.InvestorCommitments.RemoveRange(context.InvestorCommitments);
                context.SystemNotifications.RemoveRange(context.SystemNotifications);
                context.SystemReports.RemoveRange(context.SystemReports);
                context.RoiContracts.RemoveRange(context.RoiContracts);
                context.Investors.RemoveRange(context.Investors);
                context.Projects.RemoveRange(context.Projects);

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during operational data purge: {ex.Message}");
            }
            */

            // Ensure default project "Current Operations" exists
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
