using IMS.API.DTOs.Investor;
using IMS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.NetworkInformation;

namespace IMS.API.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class LookupsController : ControllerBase
    {
        //private readonly ILookupService _lookupService;
        private readonly IUnitOfWork _unitOfWork;

        public LookupsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllLookups()
        {
            // start repository calls concurrently
            var investorTypesTask = await _unitOfWork.InvestorTypes.GetAllAsync();
            var roiRangesTask = await _unitOfWork.RoiRanges.GetAllAsync();
            var roiTypesTask = await _unitOfWork.RoiTypes.GetAllAsync();

            //await Task.WhenAll(investorTypesTask, roiRangesTask, roiTypesTask);

            if (investorTypesTask == null || roiRangesTask == null || roiTypesTask == null)
            {
                return StatusCode(500, "Internal Server error occured.");
            }

            var investorTypes = investorTypesTask
                .Select(t => new LookUpItemDTO { Value = t.Id, Text = t.Name! }).ToList();

            var roiRanges = roiRangesTask
                .Select(r => new LookUpItemDTO { Value = r.Id, Text = r.DisplayLabel ?? r.Percentage.ToString() }).ToList();

            var roiTypes = roiTypesTask
                .Select(t => new LookUpItemDTO { Value = t.Id, Text = t.Name! }).ToList();

            //var banks = new List<LookUpItemDTO>
            //{
            //    new LookUpItemDTO { Value = 1, Text = "JPMorgan Chase" },
            //    new LookUpItemDTO { Value = 2, Text = "Bank of America" },
            //    new LookUpItemDTO { Value = 3, Text = "Wells Fargo" },
            //    new LookUpItemDTO { Value = 4, Text = "Citigroup" },
            //    new LookUpItemDTO { Value = 5, Text = "Goldman Sachs" }
            //};

            var response = new LookUpCollectionDTO
            {
                InvestorTypes = investorTypes,
                RoiRanges = roiRanges,
                RoiTypes = roiTypes
            };
            return Ok(response);
        }

        //[HttpGet("/investor-types")]
        //public async Task<IActionResult> GetAllInvestorTypes()
        //{
        //    var investTypes = _lookupService.AllInvestorTypes();
        //    if (investTypes == null)
        //    {
        //        return StatusCode(500, "Internal Server error occured.");
        //    }
        //    return Ok(investTypes);
        //}

        //[HttpGet("/invest-interest")]
        //public async Task<IActionResult> GetAllInvestmentInterests() 
        //{
        //    var invInterests = _lookupService.AllInvestmentInterests();

        //    if (invInterests == null)
        //    {
        //        return StatusCode(500, "Internal Server error occured.");
        //    }

        //    return Ok(invInterests);
        //}

        //[HttpGet("/roi-options")]
        //public IActionResult GetRoiOptions()
        //{
        //    var roiOptions = _lookupService.AllRoiRanges();
        //    return Ok(roiOptions);
            
        //    //return Ok(new[]
        //    //{
        //    //    new { id = 1, name = "5.0% Fixed Min" },
        //    //    new { id = 2, name = "7.5% Reserved" },
        //    //    new { id = 3, name = "10.0% Preferred" },
        //    //    new { id = 4, name = "12.5% Growth" }
        //    //});
        //}

        //[HttpGet("investor-types")]
        //public IActionResult GetInvestorTypesNew()
        //{
        //    var investorTypes = _lookupService.AllInvestorTypes();
        //    return Ok(investorTypes);

        //    //return Ok(new[]
        //    //{
        //    //    new { value = 1, text = "Individual" },
        //    //    new { value = 2, text = "Business" }
        //    //});
        //}

        //[HttpGet("investment-interests")]
        //public IActionResult GetInvestmentInterestsNew()
        //{
        //    return Ok(new[]
        //    {
        //        new { value = 1, text = "50,000 - 100,000" },
        //        new { value = 2, text = "100,000 - 500,000" },
        //        new { value = 3, text = "500,000 - 1,000,000" },
        //        new { value = 4, text = "1,000,000+" }
        //    });
        //}

        //[HttpGet("roi-ranges")]
        //public IActionResult GetRoiRanges()
        //{
        //    return Ok(new[]
        //    {
        //        new { value = 1, text = "5.0% Fixed Minimum" },
        //        new { value = 2, text = "7.5% Target Conservative" },
        //        new { value = 3, text = "10.0% Growth Dynamic" },
        //        new { value = 4, text = "12.5% High-Yield Aggressive" }
        //    });
        //}

        //[HttpGet("roi-types")]
        //public IActionResult GetRoiTypes()
        //{
        //    return Ok(new[]
        //    {
        //        new { value = 1, text = "Fixed" },
        //        new { value = 2, text = "Half-Yearly" },
        //        new { value = 3, text = "Quarterly" },
        //        new { value = 4, text = "Monthly" }
        //    });
        //}

        //[HttpGet("banks")]
        //public IActionResult GetBanks()
        //{
        //    return Ok(new[]
        //    {
        //        new { value = 1, text = "JPMorgan Chase" },
        //        new { value = 2, text = "Bank of America" },
        //        new { value = 3, text = "Wells Fargo" },
        //        new { value = 4, text = "Citigroup" },
        //        new { value = 5, text = "Goldman Sachs" }
        //    });
        //}
    }
}
