using MaverickBank.DTOs.Loan;
using MaverickBank.Services.Loan;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MaverickBank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoansController : ControllerBase
    {
        private readonly ILoanService _loanService;

        public LoansController(ILoanService loanService)
        {
            _loanService = loanService;
        }

        [HttpPost("apply")]
        public async Task<ActionResult<LoanResponseDto>> ApplyLoan(ApplyLoanDto dto)
        {
            var loan =await _loanService.ApplyLoanAsync(dto);

            return Ok(loan);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult<IEnumerable<LoanResponseDto>>>GetLoansByUserId(int userId)
        {
            var loans =await _loanService.GetLoansByUserIdAsync(userId);

            return Ok(loans);
        }

        [HttpGet("{loanId:int}")]
        public async Task<ActionResult<LoanResponseDto>>GetLoanById(int loanId)
        {
            var loan =await _loanService.GetLoanByIdAsync(loanId);

            if (loan is null)
                return NotFound();

            return Ok(loan);
        }

        [HttpPut("{loanApplicationId:int}/approve")]
        public async Task<IActionResult>ApproveLoan(int loanApplicationId,ApproveLoanDto dto)
        {
            var approved =await _loanService.UpdateLoanStatusAsync(loanApplicationId,dto);

            if (!approved)
                return NotFound();

            return NoContent();
        }

        [HttpPost("repay")]
        public async Task<IActionResult> RepayLoan(LoanRepaymentDto dto)
        {
            var repaid = await _loanService.RepayLoanAsync(dto);

            if (!repaid)
                return NotFound();

            return NoContent();
        }
    }
}
