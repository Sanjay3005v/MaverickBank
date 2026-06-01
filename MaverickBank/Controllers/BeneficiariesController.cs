using MaverickBank.DTOs.Beneficiary;
using MaverickBank.Services.Beneficiary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MaverickBank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BeneficiariesController : ControllerBase
    {
        private readonly IBeneficiaryService _beneficiaryService;

        public BeneficiariesController(IBeneficiaryService beneficiaryService)
        {
            _beneficiaryService = beneficiaryService;
        }

        [HttpPost]
        public async Task<ActionResult<BeneficiaryResponseDto>> AddBeneficiary(AddBeneficiaryDto dto)
        {
            var beneficiary = await _beneficiaryService.AddBeneficiaryAsync(dto);

            return Ok(beneficiary);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult<IEnumerable<BeneficiaryResponseDto>>> GetByUserId(int userId)
        {
            var beneficiaries = await _beneficiaryService.GetBeneficiariesByUserIdAsync(userId);

            return Ok(beneficiaries);
        }

        [HttpDelete("{beneficiaryId:int}")]
        public async Task<IActionResult> DeleteBeneficiary(int beneficiaryId)
        {
            var deleted = await _beneficiaryService.DeleteBeneficiaryAsync(beneficiaryId);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
