using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IMS.API.Controllers.Admin
{
    [Route("api/admin/documents")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class InvestorDocumentsController : ControllerBase
    {
        private readonly IInvestorDocumentService _documentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;

        public InvestorDocumentsController(IInvestorDocumentService investorDocumentService, UserManager<ApplicationUser> userManager, IWebHostEnvironment env, ApplicationDbContext context)
        {
            _documentService = investorDocumentService;
            _userManager = userManager;
            _env = env;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllInvestorDocs()
        {
            var isInvestor = User.IsInRole("investor") || User.IsInRole("Investor") || User.IsInRole("client") || User.IsInRole("Client");
            if (isInvestor && !User.IsInRole("admin") && !User.IsInRole("Admin") && !User.IsInRole("manager") && !User.IsInRole("Manager"))
            {
                var user = await _userManager.GetUserAsync(User);
                var userId = user?.Id ?? User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = user?.Email ?? User.FindFirstValue("name") ?? User.FindFirstValue(ClaimTypes.Email);
                var investorId = user?.InvestorId ?? 0;
                var investDocs = await _documentService.GetInvestorDocsByInvestorIdAsync(investorId, userId, email);
                return Ok(investDocs);
            }
            else
            {
                var investDocs = await _documentService.GetAllInvestorDocs();
                return Ok(investDocs);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvestorDocs(int id)
        {
            if (User.IsInRole("investor"))
            {
                var user = await _userManager.GetUserAsync(User);
                var investorId = user?.InvestorId ?? 0;
                if (investorId != id)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "Cannot access documents of another investor.");
                }
            }
            var investDocs = await _documentService.GetInvestorDocsByInvestorIdAsync(id);
            return Ok(investDocs);
        }

        [HttpPost]
        [Authorize(Policy = "ElevatedOrManager")]
        public async Task<IActionResult> UploadInvestorDoc([FromQuery] int id, [FromQuery] string? targetIds, [FromForm] string title, [FromForm] string type, IFormFile file)
        {
            var adminUserId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(adminUserId))
            {
                return Unauthorized("Unable to resolve Admin User Identity");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Create uploads directory if it does not exist
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Save the file
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/{uniqueFileName}";
            var fileSizeStr = $"{(file.Length / (1024.0 * 1024.0)):F1} MB";

            var dto = new UploadDocumentDTO
            {
                title = string.IsNullOrEmpty(title) ? file.FileName : title,
                type = type,
                size = fileSizeStr,
                url = fileUrl,
                uploaded_by = adminUserId
            };

            var targetInvestorIds = new List<int>();
            if (!string.IsNullOrWhiteSpace(targetIds))
            {
                var parts = targetIds.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (int.TryParse(p.Trim(), out var parsedId) && parsedId > 0)
                    {
                        targetInvestorIds.Add(parsedId);
                    }
                }
            }

            if (targetInvestorIds.Count == 0)
            {
                if (id > 0)
                {
                    targetInvestorIds.Add(id);
                }
                else
                {
                    // Target all active investors
                    var activeInvestorIds = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                        _context.Investors.AsNoTracking()
                            .Join(_context.Users.AsNoTracking(), inv => inv.OwnerUserId, u => u.Id, (inv, u) => new { inv, u })
                            .Where(x => x.u.IsActive)
                            .Select(x => x.inv.InvestorId ?? 0)
                            .Where(invId => invId > 0)
                    );

                    if (activeInvestorIds.Any())
                    {
                        targetInvestorIds.AddRange(activeInvestorIds);
                    }
                    else
                    {
                        targetInvestorIds.Add(0);
                    }
                }
            }

            foreach (var invId in targetInvestorIds.Distinct())
            {
                await _documentService.UploadDocumentMetadataAsync(invId, dto);
            }

            return Ok(new { message = "Document uploaded successfully.", url = fileUrl, recipientCount = targetInvestorIds.Count });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "ElevatedOrManager")]
        public async Task<IActionResult> DeleteInvestorDoc(int id)
        {
            var success = await _documentService.DeleteDocumentAsync(id);
            if (success)
            {
                return Ok(new { success = true });
            }
            return NotFound(new { message = "Document not found." });
        }

        [HttpPost("{id}/sign")]
        public async Task<IActionResult> SignInvestorDoc(int id, [FromBody] SignDocumentDTO dto)
        {
            var doc = await _documentService.GetInvestorDocByIdAsync(id);
            if (doc == null)
            {
                return NotFound(new { message = "Document not found." });
            }

            var isInvestor = User.IsInRole("investor") || User.IsInRole("Investor") || User.IsInRole("client") || User.IsInRole("Client");
            if (isInvestor && !User.IsInRole("admin") && !User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                var userId = user?.Id ?? User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // An investor can own multiple investment contracts/records
                var userInvestorIds = await _context.Investors
                    .Where(i => i.OwnerUserId == userId && i.InvestorId.HasValue)
                    .Select(i => i.InvestorId!.Value)
                    .ToListAsync();

                if (user?.InvestorId.HasValue == true && user.InvestorId.Value > 0 && !userInvestorIds.Contains(user.InvestorId.Value))
                {
                    userInvestorIds.Add(user.InvestorId.Value);
                }

                if (!userInvestorIds.Contains(doc.InvestorId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "Cannot sign document of another investor.");
                }
            }

            var sigData = dto?.signatureData ?? dto?.signatureName ?? "Digital Signature";
            var success = await _documentService.UpdateDocumentSignatureAsync(id, sigData);
            if (success)
            {
                return Ok(new { success = true, message = "Document digitally signed successfully." });
            }
            return BadRequest(new { message = "Could not sign document." });
        }

        [HttpPost("{id}/reset")]
        public async Task<IActionResult> ResetInvestorDoc(int id)
        {
            var success = await _documentService.ResetDocumentSignatureAsync(id);
            if (success)
            {
                return Ok(new { success = true, message = "Document signature reset to Pending Signature." });
            }
            return BadRequest(new { message = "Could not reset document." });
        }
    }

    public class SignDocumentDTO
    {
        public string? signatureName { get; set; }
        public string? signatureData { get; set; }
    }
}
