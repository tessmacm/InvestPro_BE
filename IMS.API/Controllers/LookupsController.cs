using IMS.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IMS.API.Controllers
{
    [Route("api/lookups")]
    [ApiController]
    public class LookupsController : ControllerBase
    {
        private readonly ILookupService _lookupService;

        public LookupsController(ILookupService lookupService)
        {
            _lookupService = lookupService;
        }

        [HttpGet("/investor-types")]
        public async Task<IActionResult> GetAllInvestorTypes()
        {
            var investTypes = _lookupService.AllInvestorTypes();
            if (investTypes == null)
            {
                return StatusCode(500, "Internal Server error occured.");
            }
            return Ok(investTypes);
        }

        [HttpGet("/invest-interest")]
        public async Task<IActionResult> GetAllInvestmentInterests() 
        {
            var invInterests = _lookupService.AllInvestmentInterests();

            if (invInterests == null)
            {
                return StatusCode(500, "Internal Server error occured.");
            }

            return Ok(invInterests);
        }

        [HttpGet("/roi-options")]
        public IActionResult GetRoiOptions()
        {
            return Ok(new[]
            {
                new { id = 1, name = "5.0% Fixed Min" },
                new { id = 2, name = "7.5% Reserved" },
                new { id = 3, name = "10.0% Preferred" },
                new { id = 4, name = "12.5% Growth" }
            });
        }
    }
}
