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

        public InvestorDocumentsController(IInvestorDocumentService investorDocumentService, UserManager<ApplicationUser> userManager)
        {
            _documentService = investorDocumentService;
            _userManager = userManager;
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
        public async Task<IActionResult> UploadInvestorDoc([FromQuery] int id, [FromBody] UploadDocumentDTO dto)
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(adminUserId))
            {
                return Unauthorized("Unable to resolve Admin User Identity");
            }
            
            dto.uploaded_by = adminUserId;

            var success = await _documentService.UploadDocumentMetadataAsync(id, dto);
            return Ok(new { message = "Document uploaded successfully." });
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
