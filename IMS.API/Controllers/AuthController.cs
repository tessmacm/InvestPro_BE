using IMS.API.DTOs.Auth;
using IMS.API.Services.EmailService;
using IMS.Core.Entities;
using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace IMS.API.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry)> _loginOtps = new();
        public static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry, string FirstName, string LastName)> _pendingRegistrations = new();

        UserManager<ApplicationUser>? _userManager;
        SignInManager<ApplicationUser>? _signInManager;
        IEmailService _emailService;
        IUnitOfWork _unitOfWork;
        IConfiguration _configuration;
        IInvestorManagementService _investorManagementService;

        public AuthController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IInvestorManagementService investorManagementService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _investorManagementService = investorManagementService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterInvestorDTO registerModel)
        {
            try
            {
                if (ModelState.IsValid)
                {

                   var response = await _investorManagementService.RegisterAndCreateInvestorAsync(registerModel);

                    if (!response.IsSuccess)
                    {
                        return BadRequest(new { error = response.ErrorMessage });
                    }

                    var frontendUrl = "http://localhost:5078/api/Auth/"; // Replace with your actual frontend URL

                    var confirmationLink = $"{frontendUrl}/register-verify?userId={response.UserId}&token={response.VerificationToken}";

                    await _emailService.SendEmailAsync(
                        response.Email!,
                        "Email Confirmation",
                        $"Please confirm your email by clicking on the link: {confirmationLink}");


                    return Ok(new
                    {
                        Message = "OTP dispatched successfully"
                    });
                }
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error. Contact Admin");
                #region Exception Testing
                // 1. Dig deep into the inner exceptions where SQL Server hides its real error message
                //var errorMessage = ex.Message;
                //if (ex.InnerException != null)
                //{
                //    errorMessage = ex.InnerException.Message;

                //    if (ex.InnerException.InnerException != null)
                //    {
                //        errorMessage = ex.InnerException.InnerException.Message;
                //    }
                //}

                // 2. Return the real error so you can see it in your browser/Postman network panel
                //return StatusCode(500, new
                //{
                //    error = "Database Save Failed",
                //    details = errorMessage
                //});
                #endregion
            }
        }


        [HttpPost("register-verify")]
        public async Task<IActionResult> RegisterVerify(VerifyEmailDTO verifyEmailModel)
        {
            try
            {
                var user = await _userManager!.FindByEmailAsync(verifyEmailModel.Email!);
                if (user == null)
                {
                    return BadRequest(new
                    {
                        Message = "Invalid/expired code or user exists"
                    });
                }
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(verifyEmailModel.OTP!);
                var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);
                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
                if (result.Succeeded)
                {
                    return Ok(new
                    {
                        Message = "Email confirmed successfully"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Message = "Invalid Token"
                    });
                }
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server Error. Contact Admin");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO loginModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _userManager!.FindByEmailAsync(loginModel.Email!);

                    // If user is not found
                    if (user == null)
                    {
                        return Unauthorized(new
                        {
                            Message = "Invalid Credentails"
                        });

                    }

                    //Check if the email is confirmed before allowing login
                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        return Unauthorized(new
                        {
                            Message = "Please verify your email before logging in."
                        });
                    }

                    // check user status
                    if (!user.IsActive)
                    {
                        return Unauthorized(new
                        {
                            Message = "Your account is inactive. Please contact support."
                        });
                    }

                    var signInResult = await _signInManager!.CheckPasswordSignInAsync(user!, loginModel.Password!, false);

                    if (signInResult.Succeeded)
                    {
                        var roles = await _userManager.GetRolesAsync(user!);

                        //Create JWT Token
                        //Step 1: Create Claims
                        //IdentityOptions identityOptions = new IdentityOptions();
                        //var claims = new List<Claim>
                        //{
                        //    new(identityOptions.ClaimsIdentity.UserIdClaimType, user!.Id),
                        //    new(identityOptions.ClaimsIdentity.UserNameClaimType, user.UserName!),
                        //    new(identityOptions.ClaimsIdentity.RoleClaimType, roles[0])
                        //};

                        var claims = new List<Claim>()
                        {
                            // Use explicit short string literals instead of ClaimTypes constants
                            new Claim("sub", user.Id),
                            new Claim("name", user.Email!),
                            //new Claim("role", "Admin") // <-- Note the Capital "A" in Admin!
                        };

                        foreach (var role in roles)
                        {
                            claims.Add(new Claim("role", role));
                        }
                       

                        //Step 2: Creating signingKey from SecretKey
                        //var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("This is my secret key for JWT token generation"));

                        var securityKey = _configuration.GetValue<string>("JwtSettings:SecretKey") ?? "d3011f8b98bbc1aa1c4ff1a7d4864fc72d9ee150bd682cf4e612d6321f57821d";
                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));

                        //Step 3: Creating singingCredentails using singingKey with HMAC Algorithm
                        var signCred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                        //Step 4: Create JWT with singingCredentials, IdentityClaims and Expire duration
                        var jwt = new JwtSecurityToken(
                            issuer: null, // Not validating issuer
                            audience: null, // Not validating audience
                            signingCredentials: signCred,
                            claims: claims,
                            expires: DateTime.Now.AddMinutes(30));

                        //Step 5: Finally write the token as response with OK() using the new required shape.
                        var tokenString = new JwtSecurityTokenHandler().WriteToken(jwt);
                        var fullName = $"{user.FirstName} {user.LastName}".Trim();
                        if (string.IsNullOrWhiteSpace(fullName)) fullName = user.UserName ?? string.Empty;

                        return Ok(new
                        {
                            token = tokenString,
                            user = new
                            {
                                id = user.Id,
                                email = user.Email,
                                name = fullName,
                                role = roles.Count > 0 ? roles[0] : string.Empty,
                                status = user.IsActive ? "active" : "inactive"
                            }
                        });
                    }
                    else
                    {
                        return Unauthorized(new
                        {
                            Message = "Invalid Credentails"
                        });
                    }
                }
                return BadRequest(ModelState);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server Error. Contact Admin");
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO forgotPasswordModel)
        {
            try
            {
                var user = await _userManager!.FindByEmailAsync(forgotPasswordModel.Email!);
                if (user == null)
                {
                    return NotFound(new
                    {
                        Message = "User not found"
                    });
                }
                var token = await _userManager.GeneratePasswordResetTokenAsync(user!);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var frontendUrl = "http://localhost:5078/api/Auth/"; // Replace with your actual frontend URL
                var resetLink = $"{frontendUrl}/reset-password?email={user.Email}&token={encodedToken}";

                await _emailService.SendEmailAsync(
                    user.Email!,
                    "Password Reset",
                    $"Please reset your password by clicking on the link: {resetLink}");
                return Ok(new
                {
                    Message = "Password reset link dispatched successfully"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server Error. Contact Admin");
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO resetPasswordModel)
        {
            try
            {
                var user = await _userManager!.FindByEmailAsync(resetPasswordModel.Email!);
                if (user == null)
                {
                    return NotFound(new
                    {
                        Message = "User not found"
                    });
                }
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(resetPasswordModel.Token!);
                var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);
                var result = await _userManager.ResetPasswordAsync(user!, decodedToken, resetPasswordModel.NewPassword!);
                if (result.Succeeded)
                {
                    return Ok(new
                    {
                        Message = "Password reset successfully"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Message = "Invalid Token or Password does not meet requirements"
                    });
                }
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server Error. Contact Admin");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager!.SignOutAsync();
                return Ok(new
                {
                    Message = "Logged out successfully"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server Error. Contact Admin");
            }

        }

        [HttpGet("debug-my-claims")]
        [AllowAnonymous]
        public IActionResult DebugClaims()
        {
            // Grabs the Authorization string header manually
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader)) return BadRequest("No header found.");

            var token = authHeader.Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Returns a raw look at every single claim embedded in the token
            // Returns a raw look at every single claim embedded in the token
            return Ok(jwtToken.Claims.Select(c => new { c.Type, c.Value }));
        }

        [HttpGet("check-users")]
        public IActionResult CheckUsers()
        {
            var hasUsers = _userManager!.Users.Any();
            return Ok(new { hasUsers });
        }

        [HttpPost("send-login-otp")]
        public async Task<IActionResult> SendLoginOtp([FromBody] SendLoginOtpDTO model)
        {
            if (string.IsNullOrEmpty(model.Email))
            {
                return BadRequest(new { Message = "Email is required." });
            }

            var user = await _userManager!.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { Message = "Your account is inactive. Please contact support." });
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);
            _loginOtps[model.Email.ToLowerInvariant()] = (otp, expiry);

            await _emailService.SendEmailAsync(
                user.Email!,
                "Login Verification Code",
                $"Your login verification code is: {otp}. This code will expire in 10 minutes.");

            return Ok(new { Message = "OTP sent successfully" });
        }

        [HttpPost("verify-login-otp")]
        public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpDTO model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Otp))
            {
                return BadRequest(new { Message = "Email and OTP are required." });
            }

            var emailKey = model.Email.ToLowerInvariant();
            if (!_loginOtps.TryGetValue(emailKey, out var otpData))
            {
                return BadRequest(new { Message = "Invalid or expired OTP." });
            }

            if (otpData.Otp != model.Otp || DateTime.UtcNow > otpData.Expiry)
            {
                return BadRequest(new { Message = "Invalid or expired OTP." });
            }

            _loginOtps.TryRemove(emailKey, out _);

            var user = await _userManager!.FindByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { Message = "User status invalid." });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>()
            {
                new Claim("sub", user.Id),
                new Claim("name", user.Email!),
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
            }
            if (user.InvestorId.HasValue)
            {
                claims.Add(new Claim("investorId", user.InvestorId.Value.ToString()));
            }

            var securityKey = _configuration.GetValue<string>("JwtSettings:SecretKey") ?? "d3011f8b98bbc1aa1c4ff1a7d4864fc72d9ee150bd682cf4e612d6321f57821d";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
            var signCred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: null,
                audience: null,
                signingCredentials: signCred,
                claims: claims,
                expires: DateTime.Now.AddMinutes(30));

            var tokenString = new JwtSecurityTokenHandler().WriteToken(jwt);
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName)) fullName = user.UserName ?? string.Empty;

            return Ok(new
            {
                token = tokenString,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    name = fullName,
                    role = roles.Count > 0 ? roles[0] : string.Empty,
                    status = user.IsActive ? "active" : "inactive"
                }
            });
        }

        [HttpPost("register-request")]
        public async Task<IActionResult> RegisterRequest([FromBody] RegisterRequestDTO model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.FirstName) || string.IsNullOrEmpty(model.LastName))
            {
                return BadRequest(new { Message = "First name, last name, and email are required." });
            }

            var user = await _userManager!.FindByEmailAsync(model.Email);
            if (user != null)
            {
                return BadRequest(new { Message = "Email is already registered." });
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);
            _pendingRegistrations[model.Email.ToLowerInvariant()] = (otp, expiry, model.FirstName, model.LastName);

            await _emailService.SendEmailAsync(
                model.Email,
                "Registration Verification Code",
                $"Your registration verification code is: {otp}. This code will expire in 10 minutes.");

            return Ok(new { Message = "OTP sent successfully" });
        }

        [HttpPost("register-verify-otp")]
        public async Task<IActionResult> RegisterVerifyOtp([FromBody] RegisterVerifyOtpDTO model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Otp))
            {
                return BadRequest(new { Message = "Email and OTP are required." });
            }

            var emailKey = model.Email.ToLowerInvariant();
            if (!_pendingRegistrations.TryGetValue(emailKey, out var regData))
            {
                return BadRequest(new { Message = "Invalid or expired registration session." });
            }

            if (regData.Otp != model.Otp || DateTime.UtcNow > regData.Expiry)
            {
                return BadRequest(new { Message = "Invalid or expired OTP." });
            }

            _pendingRegistrations.TryRemove(emailKey, out _);

            var existingUser = await _userManager!.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return BadRequest(new { Message = "User already exists." });
            }

            var assignedRole = "admin";

            var newUser = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = regData.FirstName,
                LastName = regData.LastName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(newUser, "Password123!");
            if (!createResult.Succeeded)
            {
                return BadRequest(new { Message = createResult.Errors.FirstOrDefault()?.Description ?? "Failed to create user." });
            }

            await _userManager.AddToRoleAsync(newUser, assignedRole);

            var admins = await _userManager.GetUsersInRoleAsync("admin");
            foreach (var sa in admins)
            {
                await _emailService.SendEmailAsync(
                    sa.Email!,
                    "New User Registration Alert",
                    $"A new user has registered on the platform:\n\nName: {regData.FirstName} {regData.LastName}\nEmail: {model.Email}\nAssigned Role: {assignedRole}\n\nYou can manage this user's details and approval status in the Admin Panel.");
            }

            var roles = new List<string> { assignedRole };
            var claims = new List<Claim>()
            {
                new Claim("sub", newUser.Id),
                new Claim("name", newUser.Email!),
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
            }
            if (newUser.InvestorId.HasValue)
            {
                claims.Add(new Claim("investorId", newUser.InvestorId.Value.ToString()));
            }

            var securityKey = _configuration.GetValue<string>("JwtSettings:SecretKey") ?? "d3011f8b98bbc1aa1c4ff1a7d4864fc72d9ee150bd682cf4e612d6321f57821d";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
            var signCred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: null,
                audience: null,
                signingCredentials: signCred,
                claims: claims,
                expires: DateTime.Now.AddMinutes(30));

            var tokenString = new JwtSecurityTokenHandler().WriteToken(jwt);
            var fullName = $"{newUser.FirstName} {newUser.LastName}".Trim();

            return Ok(new
            {
                token = tokenString,
                user = new
                {
                    id = newUser.Id,
                    email = newUser.Email,
                    name = fullName,
                    role = assignedRole,
                    status = "active"
                }
            });
        }

    }

    public class SendLoginOtpDTO
    {
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyLoginOtpDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class RegisterRequestDTO
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class RegisterVerifyOtpDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }
}