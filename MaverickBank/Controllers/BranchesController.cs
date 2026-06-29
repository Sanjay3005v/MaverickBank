using Asp.Versioning;
using MaverickBank.DTOs.Branch;
using MaverickBank.Services.Branch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MaverickBank.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class BranchesController : ControllerBase
    {
        private readonly IBranchService _branchService;

        public BranchesController(IBranchService branchService)
        {
            _branchService = branchService;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<BranchResponseDto>>> GetAllBranches([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var branches = await _branchService.GetAllBranchesAsync(pageNumber, pageSize);
            return Ok(branches);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<BranchResponseDto>> GetBranchById(int id)
        {
            var branch = await _branchService.GetBranchByIdAsync(id);
            if (branch is null)
                return NotFound(new { message = $"Branch with ID {id} not found." });

            return Ok(branch);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<BranchResponseDto>> CreateBranch(CreateBranchDto dto)
        {
            var created = await _branchService.CreateBranchAsync(dto);
            return CreatedAtAction(nameof(GetBranchById), new { id = created.BranchId }, created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBranch(int id, CreateBranchDto dto)
        {
            var updated = await _branchService.UpdateBranchAsync(id, dto);
            if (!updated)
                return NotFound(new { message = $"Branch with ID {id} not found." });

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            var deleted = await _branchService.DeleteBranchAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Branch with ID {id} not found." });

            return NoContent();
        }

        [HttpGet("search")]
        [Authorize(Roles = "Customer,Admin,Employee")]
        public async Task<ActionResult<IEnumerable<BranchResponseDto>>> SearchBranches([FromQuery] string bankName)
        {
            if (string.IsNullOrWhiteSpace(bankName))
                return BadRequest(new { message = "bankName query parameter is required." });

            var branches = await _branchService.SearchBranchesByNameAsync(bankName);
            return Ok(branches);
        }
    }
}
