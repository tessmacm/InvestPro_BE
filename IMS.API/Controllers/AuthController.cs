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

namespace IMS.API.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
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
            return Ok(jwtToken.Claims.Select(c => new { c.Type, c.Value }));
        }

    }
}