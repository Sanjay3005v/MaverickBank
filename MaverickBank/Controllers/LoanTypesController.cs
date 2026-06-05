using Asp.Versioning;
using MaverickBank.DTOs.Loan;
using MaverickBank.Services.Loan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MaverickBank.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class LoanTypesController : ControllerBase
    {
        private readonly ILoanTypeService _loanTypeService;

        public LoanTypesController(ILoanTypeService loanTypeService)
        {
            _loanTypeService = loanTypeService;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<LoanTypeResponseDto>>> GetAllLoanTypes([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var loanTypes = await _loanTypeService.GetAllLoanTypesAsync(pageNumber, pageSize);
            return Ok(loanTypes);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<LoanTypeResponseDto>> GetLoanTypeById(int id)
        {
            var loanType = await _loanTypeService.GetLoanTypeByIdAsync(id);
            if (loanType is null)
                return NotFound(new { message = $"Loan type with ID {id} not found." });

            return Ok(loanType);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<LoanTypeResponseDto>> CreateLoanType(CreateLoanTypeDto dto)
        {
            var created = await _loanTypeService.CreateLoanTypeAsync(dto);
            return CreatedAtAction(nameof(GetLoanTypeById), new { id = created.LoanTypeId }, created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateLoanType(int id, CreateLoanTypeDto dto)
        {
            var updated = await _loanTypeService.UpdateLoanTypeAsync(id, dto);
            if (!updated)
                return NotFound(new { message = $"Loan type with ID {id} not found." });

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteLoanType(int id)
        {
            var deleted = await _loanTypeService.DeleteLoanTypeAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Loan type with ID {id} not found." });

            return NoContent();
        }
    }
}
