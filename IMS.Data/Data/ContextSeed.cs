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

        // 3. Ensure only one project "Current Operations" exists in the database
        if (context != null)
        {
            var existingProjects = context.Projects.ToList();
            var currentOps = existingProjects.FirstOrDefault(p => p.Title == "Current Operations");
            if (currentOps == null)
            {
                currentOps = new Project
                {
                    Title = "Current Operations",
                    Description = "Core investment portfolio management & active operations",
                    TargetFunding = 1000000.00m,
                    FundedAmount = 500000.00m,
                    LaunchDate = DateTime.UtcNow,
                    Status = "Active"
                };
                context.Projects.Add(currentOps);
                await context.SaveChangesAsync();
            }

            var redundantProjects = context.Projects.Where(p => p.Id != currentOps.Id).ToList();
            if (redundantProjects.Any())
            {
                var redundantIds = redundantProjects.Select(rp => rp.Id).ToList();
                var commitmentsToReassign = context.InvestorCommitments.Where(c => redundantIds.Contains(c.ProjectId)).ToList();
                foreach (var comm in commitmentsToReassign)
                {
                    comm.ProjectId = currentOps.Id;
                }
                context.Projects.RemoveRange(redundantProjects);
                await context.SaveChangesAsync();
            }

            // 4. Ensure agreement documents exist for all investors in the database & link user accounts
            var allInvestors = context.Investors.ToList();
            var allUsers = context.Users.ToList();

            foreach (var inv in allInvestors)
            {
                var userAccount = allUsers.FirstOrDefault(u => u.Id == inv.OwnerUserId)
                    ?? allUsers.FirstOrDefault(u => u.InvestorId == inv.InvestorId)
                    ?? allUsers.FirstOrDefault(u => !string.IsNullOrEmpty(u.Email) && !string.IsNullOrEmpty(inv.OwnerUserId) && u.Email == inv.OwnerUserId);

                if (userAccount != null)
                {
                    if (userAccount.InvestorId != inv.InvestorId)
                    {
                        userAccount.InvestorId = inv.InvestorId;
                    }
                    if (inv.OwnerUserId != userAccount.Id)
                    {
                        inv.OwnerUserId = userAccount.Id;
                    }
                }

                var hasAgreement = context.InvestorDocuments.Any(d => d.InvestorId == inv.InvestorId && (d.DocumentType == "Agreement" || d.Title!.Contains("Agreement")));
                if (!hasAgreement)
                {
                    var name = userAccount != null ? $"{userAccount.FirstName} {userAccount.LastName}".Trim() : "Investor";
                    if (string.IsNullOrWhiteSpace(name)) name = "Investor";
                    context.InvestorDocuments.Add(new InvestorDocument
                    {
                        InvestorId = inv.InvestorId ?? 0,
                        Title = $"Investment Agreement - {name} (Current Operations).pdf",
                        DocumentType = "Agreement",
                        Size = 1.2m,
                        StorageUrl = $"/documents/agreement_{inv.InvestorId}.pdf",
                        UploadedAt = DateTime.UtcNow,
                        UploadedById = userAccount?.Id ?? "system",
                        Status = "Pending Signature"
                    });
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
