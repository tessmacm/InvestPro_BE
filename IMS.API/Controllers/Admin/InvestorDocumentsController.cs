using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        public InvestorDocumentsController(IInvestorDocumentService investorDocumentService, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _documentService = investorDocumentService;
            _userManager = userManager;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllInvestorDocs()
        {
            if (User.IsInRole("investor"))
            {
                var user = await _userManager.GetUserAsync(User);
                var investorId = user?.InvestorId ?? 0;
                var investDocs = await _documentService.GetInvestorDocsByInvestorIdAsync(investorId);
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
        public async Task<IActionResult> UploadInvestorDoc([FromQuery] int id, [FromForm] string title, [FromForm] string type, IFormFile file)
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

            var success = await _documentService.UploadDocumentMetadataAsync(id, dto);
            return Ok(new { message = "Document uploaded successfully.", url = fileUrl });
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
    }
}
