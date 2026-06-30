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

        [HttpGet("investor-types")]
        public IActionResult GetInvestorTypesNew()
        {
            return Ok(new[]
            {
                new { value = 1, text = "Individual" },
                new { value = 2, text = "Business" }
            });
        }

        [HttpGet("investment-interests")]
        public IActionResult GetInvestmentInterestsNew()
        {
            return Ok(new[]
            {
                new { value = 1, text = "50,000 - 100,000" },
                new { value = 2, text = "100,000 - 500,000" },
                new { value = 3, text = "500,000 - 1,000,000" },
                new { value = 4, text = "1,000,000+" }
            });
        }

        [HttpGet("roi-ranges")]
        public IActionResult GetRoiRanges()
        {
            return Ok(new[]
            {
                new { value = 1, text = "5.0% Fixed Minimum" },
                new { value = 2, text = "7.5% Target Conservative" },
                new { value = 3, text = "10.0% Growth Dynamic" },
                new { value = 4, text = "12.5% High-Yield Aggressive" }
            });
        }

        [HttpGet("roi-types")]
        public IActionResult GetRoiTypes()
        {
            return Ok(new[]
            {
                new { value = 1, text = "Fixed" },
                new { value = 2, text = "Half-Yearly" },
                new { value = 3, text = "Quarterly" },
                new { value = 4, text = "Monthly" }
            });
        }

        [HttpGet("banks")]
        public IActionResult GetBanks()
        {
            return Ok(new[]
            {
                new { value = 1, text = "JPMorgan Chase" },
                new { value = 2, text = "Bank of America" },
                new { value = 3, text = "Wells Fargo" },
                new { value = 4, text = "Citigroup" },
                new { value = 5, text = "Goldman Sachs" }
            });
        }
    }
}
