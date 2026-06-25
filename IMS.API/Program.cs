using IMS.API.Services.EmailService;
using IMS.Core.Interfaces;
using IMS.Core.Entities;
using IMS.Persistance.Data;
using IMS.Persistance.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

//JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);
IConfiguration configuration = builder.Configuration;

var conString = configuration.GetConnectionString("DefaultConnection");

var jwtSettings = configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "d3011f8b98bbc1aa1c4ff1a7d4864fc72d9ee150bd682cf4e612d6321f57821d";

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Disable camelCase in JSON output, preserve property names as defined in C# classes
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Register Authentication with JWT Bearer scheme
builder.Services.AddAuthentication(options =>
{
    // Set the default scheme used for authentication — this means how the app will try to authenticate incoming requests
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

    // Set the default challenge scheme — this is how the app will challenge unauthorized requests
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        // Prevents .NET from renaming your "role" claims under the hood
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Do NOT validate the issuer (the token's "iss" claim)
            ValidateIssuer = false,

            // Do NOT validate the audience (the token's "aud" claim)
            ValidateAudience = false,

            // Ensure the token's signature matches the signing key (to verify token integrity)
            ValidateIssuerSigningKey = true,

            RoleClaimType = "role",

            // The key used to sign tokens — must match the key used to generate tokens
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)) // Use a symmetric key from configuration for token validation.
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Define a policy named "AdminOnly" that requires the user to have the "Admin" role.
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

// Elevator Rights Admin/Super-Admin
    options.AddPolicy("ElevatedRights", policy =>
    policy.RequireRole("admin", "superadmin"));

    options.AddPolicy("SuperAdminOnly", policy =>
       policy.RequireRole("superadmin"));
        
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(conString)
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
builder.Services.AddIdentity<ApplicationUser,IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

builder.Services.AddCors();

builder.Services.AddScoped<IUnitOfWork,UnitOfWork>();

// Service Registration goes here
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAdminManagementService, AdminManagementService>();
builder.Services.AddScoped<IInvestorManagementService, InvestorManagementService>();
builder.Services.AddScoped<IInvestorDocumentService, InvestorDocumentService>();


// Http Pipeline

var app = builder.Build();
// Configure the HTTP request pipeline.

using (var scope = app.Services.CreateScope())
{
    #region Creating Roles
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roleNames = configuration.GetSection("Roles").GetChildren().Select(x => x.Value).ToArray()!;

    foreach (var roleName in roleNames)
    {
        var roleExist = roleManager.RoleExistsAsync(roleName).Result;
        if (!roleExist)
        {
            var roleResult = roleManager.CreateAsync(new IdentityRole(roleName)).Result;
        }
    }
    #endregion
    #region Creating Admin
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminUser = configuration.GetSection("defaultAdminUser").Value!;
    var adminPassword = configuration.GetSection("defaultAdminPassword").Value!;
    var adminDefaultRole = configuration.GetSection("defaultAdminRole").Value!;
    var userExist = userManager.FindByEmailAsync(adminUser).Result;

    if (userExist == null)
    {
        var saUser = new ApplicationUser() { UserName = adminUser, Email = adminUser, EmailConfirmed = true };
        var userResult = userManager.CreateAsync(saUser,adminPassword).Result;
        var defaultRoleResult = userManager.AddToRoleAsync(saUser, adminDefaultRole).Result;
    }
    else if (!userExist.EmailConfirmed)
    {
        userExist.EmailConfirmed = true;
        var updateResult = userManager.UpdateAsync(userExist).Result;
    }

    #endregion 

    #region Custom Seed Data (Projects, Investors, Documents)
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Seed Projects
    if (!dbContext.Projects.Any())
    {
        dbContext.Projects.AddRange(
            new Project { Title = "InvestPro Mobile App", Description = "A modern mobile application for investors to track portfolios.", TargetFunding = 250000, FundedAmount = 0, LaunchDate = new DateTime(2024, 5, 1), Status = "Open" },
            new Project { Title = "Investor Dashboard Redesign", Description = "Revamping the client-facing investor dashboard with modern UI/UX components.", TargetFunding = 180000, FundedAmount = 0, LaunchDate = new DateTime(2024, 4, 10), Status = "Open" },
            new Project { Title = "Payment Gateway Integration", Description = "Enable multi-currency seamless deposits and withdrawals.", TargetFunding = 300000, FundedAmount = 0, LaunchDate = new DateTime(2024, 2, 5), Status = "Open" }
        );
        dbContext.SaveChanges();
    }

    // Seed an Investor
    if (!dbContext.Investors.Any())
    {
        var userManagerInstance = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var investorEmail = "john@example.com";
        var investorUser = userManagerInstance.FindByEmailAsync(investorEmail).Result;
        
        if (investorUser == null)
        {
            investorUser = new ApplicationUser
            {
                UserName = investorEmail,
                Email = investorEmail,
                FirstName = "John",
                LastName = "Doe",
                EmailConfirmed = true,
                PhoneNumber = "+1234567890",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var createRes = userManagerInstance.CreateAsync(investorUser, "Password123!").Result;
            if (createRes.Succeeded)
            {
                userManagerInstance.AddToRoleAsync(investorUser, "Investor").Wait();
                
                var investor = new Investor
                {
                    OwnerUserId = investorUser.Id,
                    DateOfBirth = DateTime.UtcNow.AddYears(-25),
                    TaxIdOrSSN = "United States",
                    LegalBusinessName = "Doe Holdings LLC",
                    CompanyRegistrationNo = "REG-993812",
                    AuthorizedSignerName = "Accredited",
                    CapitalAmount = 500000,
                    Notes = "Venture Capital",
                    InvestorTypeId = 1,
                    InvestmentInterestId = 3
                };
                dbContext.Investors.Add(investor);
                dbContext.SaveChanges();

                investorUser.InvestorId = investor.InvestorId;
                userManagerInstance.UpdateAsync(investorUser).Wait();
                
                // Seed a Document for this investor
                var adminUserObj = userManagerInstance.FindByEmailAsync(adminUser).Result;
                var adminId = adminUserObj?.Id;
                dbContext.InvestorDocuments.Add(new InvestorDocument
                {
                    InvestorId = investor.InvestorId ?? 1,
                    Title = "Passport Verification",
                    DocumentType = "PDF",
                    Size = 1.2m,
                    StorageUrl = "#",
                    UploadedById = adminId ?? investorUser.Id,
                    UploadedAt = DateTime.UtcNow.AddDays(-2),
                    Status = "Approved"
                });
                dbContext.SaveChanges();
            }
        }
    }
    #endregion
}

// Default Settings goes here Roles, Admin, etd.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(x => x.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader());
    

app.UseAuthentication();
app.UseAuthorization();

//Exception Handling Middleware

app.UseExceptionHandler(options =>
{ 
    options.Run(async context =>
    {
        var ex = context.Features.Get<IExceptionHandlerFeature>();

        if (ex !=null)
        {
            //ex.Error
            context.Response.StatusCode = 500; // Internal Server Error
            context.Response.ContentType = "application/json"; 
            var msg =(ex.Error.InnerException != null) ? ex.Error.InnerException.Message : ex.Error.Message;
            // log this message
            await context.Response.WriteAsync("Admin is working on it at application level " + msg);
        }
    });
});

app.MapControllers();

//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;

//    try
//    {
//        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
//        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

//        // Seed Roles and Admin User
//        await ContextSeed.SeedRolesAndAdminAdync(userManager, roleManager);
//    }
//    catch (Exception ex)
//    {
//      var logger = services.GetRequiredService<ILogger<Program>>();
//      logger.LogError(ex, "An error occurred seeding the DB.");
//    }
//}

app.Run();
