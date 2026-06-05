using Asp.Versioning;
using MaverickBank.DTOs.Loan;
using MaverickBank.DTOs.Pagination;
using MaverickBank.DTOs.Transaction;
using MaverickBank.Services.Loan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaverickBank.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class LoansController : ControllerBase
    {
        private readonly ILoanService _loanService;

        public LoansController(ILoanService loanService)
        {
            _loanService = loanService;
        }

        [HttpPost("apply")]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<LoanResponseDto>> ApplyLoan(ApplyLoanDto dto)
        {
            var loan = await _loanService.ApplyLoanAsync(dto);

            return Ok(loan);
        }

        [HttpGet("user/{userId:int}")]
        [Authorize(Roles = "Customer,Employee,Admin")]
        public async Task<ActionResult<IEnumerable<LoanResponseDto>>> GetLoansByUserId(int userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var loans = await _loanService.GetLoansByUserIdAsync(userId, pageNumber, pageSize);

            return Ok(loans);
        }

        [HttpGet("{loanId:int}")]
        public async Task<ActionResult<LoanResponseDto>> GetLoanById(int loanId)
        {
            var loan = await _loanService.GetLoanByIdAsync(loanId);

            if (loan is null)
                return NotFound(new { message = $"Loan with ID {loanId} not found." });

            return Ok(loan);
        }

        [HttpPut("{loanApplicationId:int}/approve")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> ApproveLoan(int loanApplicationId, ApproveLoanDto dto)
        {
            var approved = await _loanService.UpdateLoanStatusAsync(loanApplicationId, dto);

            if (!approved)
                return NotFound(new { message = $"Loan application with ID {loanApplicationId} not found." });

            return NoContent();
        }

        [HttpPost("repay")]
        public async Task<IActionResult> RepayLoan(LoanRepaymentDto dto)
        {
            var repaid = await _loanService.RepayLoanAsync(dto);

            if (!repaid)
                return NotFound(new { message = "Loan not found." });

            return NoContent();
        }

        [HttpPut("{loanApplicationId:int}/reject")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> RejectLoan(int loanApplicationId, RejectLoanDto dto)
        {
            var rejected = await _loanService.RejectLoanAsync(loanApplicationId, dto);

            if (!rejected)
                return NotFound(new { message = $"Loan application with ID {loanApplicationId} not found." });

            return NoContent();
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<ActionResult<PagedResultDto<LoanResponseDto>>> GetPendingLoanApplications(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var loans = await _loanService.GetPendingLoanApplicationsAsync(pageNumber, pageSize);
            return Ok(loans);
        }
    }
}
