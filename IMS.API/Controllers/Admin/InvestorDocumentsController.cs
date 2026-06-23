using IMS.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Security.Claims;
using System.Threading.Tasks.Dataflow;


namespace IMS.API.Controllers.Admin
{
    [Route("api/admin/documents")]
    [ApiController]
    public class InvestorDocumentsController : ControllerBase
    {
        private readonly IInvestorDocumentService _documentService;

        public InvestorDocumentsController(IInvestorDocumentService investorDocumentService)
        {
            _documentService = investorDocumentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllInvestorDocs()
        {
            var investDocs = _documentService.GetAllInvestorDocs();
            return Ok(investDocs);
        }

        [HttpGet("/{id}")]
        public async Task<IActionResult> GetInvestorDocs(int id)
        {
            var investDocs = _documentService.GetInvestorDocsByInvestorIdAsync(id);
            return Ok(investDocs);
        }

        [HttpPost]
        public async Task<IActionResult> UploadInvestorDoc(int id, [FromBody] UploadDocumentDTO dto)
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(adminUserId))
            {
                return Unauthorized("Unable to resolve Admin User Identity");
            }
            
            dto.Uploaded_By = adminUserId;

            var success = _documentService.UploadDocumentMetadataAsync(id, dto);
            return Ok(new { message = "Document uploaded successfully." });
  
        }

        
    }
}
