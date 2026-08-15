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

builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
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

            ClockSkew = TimeSpan.FromMinutes(5),

            // The key used to sign tokens — must match the key used to generate tokens
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)) // Use a symmetric key from configuration for token validation.
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Define a policy named "AdminOnly" that requires the user to have the "admin" or "superadmin" role.
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin", "Admin", "superadmin", "SuperAdmin"));

    // Elevated Rights: Admin
    options.AddPolicy("ElevatedRights", policy =>
        policy.RequireRole("admin", "Admin", "superadmin", "SuperAdmin"));

    options.AddPolicy("ElevatedOrManager", policy =>
        policy.RequireRole("admin", "Admin", "manager", "Manager", "superadmin", "SuperAdmin"));

    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole("admin", "Admin", "superadmin", "SuperAdmin"));
        
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
    // Default admin and custom seed data removed to support completely clean database on startup.
    #region Creating Admin
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminUser = configuration.GetSection("defaultAdminUser").Value!;
    var adminPassword = configuration.GetSection("defaultAdminPassword").Value!;
    var adminDefaultRole = configuration.GetSection("defaultAdminRole").Value!;
    var userExist = userManager.FindByEmailAsync(adminUser).Result;

    if (userExist == null)
    {
        var saUser = new ApplicationUser() { UserName = adminUser, Email = adminUser };
        var userResult = userManager.CreateAsync(saUser, adminPassword).Result;
        var defaultRoleResult = userManager.AddToRoleAsync(saUser, adminDefaultRole).Result;
    }

    #endregion 
}

// Default Settings goes here Roles, Admin, etd.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();

app.UseCors(x => x.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader());

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

//Exception Handling Middleware

app.UseExceptionHandler(options =>
{ 
    options.Run(async context =>
    {
        var ex = context.Features.Get<IExceptionHandlerFeature>();

        if (ex != null)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json"; 
            var msg = (ex.Error.InnerException != null) ? ex.Error.InnerException.Message : ex.Error.Message;
            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(new { message = $"Internal Server Error: {msg}" });
            await context.Response.WriteAsync(jsonResponse);
        }
    });
});

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Ensure baseline migration is registered as applied since the tables already exist
        context.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260703061105_ModifyInvestorRRRT')
            BEGIN
                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) 
                VALUES ('20260703061105_ModifyInvestorRRRT', '9.0.0')
            END

            IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'InvestorTypes')
            BEGIN
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'InvestorTypes' AND COLUMN_NAME = 'Status')
                BEGIN
                    ALTER TABLE InvestorTypes ADD Status NVARCHAR(50) NOT NULL DEFAULT 'active';
                END
            END

            IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RoiRanges')
            BEGIN
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RoiRanges' AND COLUMN_NAME = 'Status')
                BEGIN
                    ALTER TABLE RoiRanges ADD Status NVARCHAR(50) NOT NULL DEFAULT 'active';
                END
            END

            IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RoiTypes')
            BEGIN
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RoiTypes' AND COLUMN_NAME = 'Status')
                BEGIN
                    ALTER TABLE RoiTypes ADD Status NVARCHAR(50) NOT NULL DEFAULT 'active';
                END
            END

            IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'InvestorDocuments')
            BEGIN
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'InvestorDocuments' AND COLUMN_NAME = 'SignatureData')
                BEGIN
                    ALTER TABLE InvestorDocuments ADD SignatureData NVARCHAR(MAX) NULL;
                END
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'InvestorDocuments' AND COLUMN_NAME = 'SignedAt')
                BEGIN
                    ALTER TABLE InvestorDocuments ADD SignedAt DATETIME2 NULL;
                END
            END

            IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Investors')
            BEGIN
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Investors' AND COLUMN_NAME = 'Address')
                BEGIN
                    ALTER TABLE Investors ADD Address NVARCHAR(MAX) NULL;
                END
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Investors' AND COLUMN_NAME = 'Witness')
                BEGIN
                    ALTER TABLE Investors ADD Witness NVARCHAR(255) NULL;
                END
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Investors' AND COLUMN_NAME = 'PayoutType')
                BEGIN
                    ALTER TABLE Investors ADD PayoutType NVARCHAR(100) NULL;
                END
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Investors' AND COLUMN_NAME = 'ProjectId')
                BEGIN
                    ALTER TABLE Investors ADD ProjectId INT NULL;
                END
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Investors' AND COLUMN_NAME = 'Duration')
                BEGIN
                    ALTER TABLE Investors ADD Duration NVARCHAR(100) NULL DEFAULT '12 Months';
                END
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Investors' AND COLUMN_NAME = 'CreatedAt')
                BEGIN
                    ALTER TABLE Investors ADD CreatedAt DATETIME2 NULL DEFAULT GETUTCDATE();
                END
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SystemNotifications')
                BEGIN
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SystemNotifications' AND COLUMN_NAME = 'SenderUserId')
                    BEGIN
                        ALTER TABLE SystemNotifications ADD SenderUserId NVARCHAR(450) NULL;
                    END
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SystemNotifications' AND COLUMN_NAME = 'SenderName')
                    BEGIN
                        ALTER TABLE SystemNotifications ADD SenderName NVARCHAR(255) NULL;
                    END
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SystemNotifications' AND COLUMN_NAME = 'SenderRole')
                    BEGIN
                        ALTER TABLE SystemNotifications ADD SenderRole NVARCHAR(100) NULL;
                    END
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SystemNotifications' AND COLUMN_NAME = 'ReadAt')
                    BEGIN
                        ALTER TABLE SystemNotifications ADD ReadAt DATETIME2 NULL;
                    END
                END
            END
        ");

        // Apply new pending migrations
        context.Database.Migrate();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Seed Roles, Admin User, and Current Operations project
        await ContextSeed.SeedRolesAndAdminAdync(userManager, roleManager, context);
    }
    catch (Exception ex)
    {
      var logger = services.GetRequiredService<ILogger<Program>>();
      logger.LogError(ex, "An error occurred seeding or migrating the DB.");
    }
}

app.Run();
