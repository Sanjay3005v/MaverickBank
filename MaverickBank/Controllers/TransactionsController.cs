using Asp.Versioning;
using MaverickBank.DTOs.Pagination;
using MaverickBank.DTOs.Transaction;
using MaverickBank.Extensions;
using MaverickBank.Services.Account;
using MaverickBank.Services.Transaction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MaverickBank.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IAccountService _accountService;

        public TransactionsController(ITransactionService transactionService, IAccountService accountService)
        {
            _transactionService = transactionService;
            _accountService = accountService;
        }

        [HttpPost("deposit")]
        [Authorize(Roles = "Customer,Employee")]
        public async Task<ActionResult<TransactionResponseDto>> Deposit(DepositDto dto)
        {
            if (!User.IsStaff())
            {
                var account = await _accountService.GetAccountByIdAsync(dto.AccountId);
                if (account is null)
                    return NotFound(new { message = "Account not found." });

                if (!User.CanAccessUser(account.UserId))
                    return Forbid();
            }
            var transaction = await _transactionService.DepositAsync(dto);

            return Ok(transaction);
        }

        [HttpPost("withdraw")]
        [Authorize(Roles = "Customer,Employee")]
        public async Task<ActionResult<TransactionResponseDto>> Withdraw(WithdrawDto dto)
        {
            if (!User.IsStaff())
            {
                var account = await _accountService.GetAccountByIdAsync(dto.AccountId);
                if (account is null)
                    return NotFound(new { message = "Account not found." });

                if (!User.CanAccessUser(account.UserId))
                    return Forbid();
            }
            var transaction = await _transactionService.WithdrawAsync(dto);

            return Ok(transaction);
        }


        [HttpPost("transfer")]
        [Authorize(Roles = "Customer,Employee")]
        public async Task<ActionResult<TransactionResponseDto>> Transfer(TransferDto dto)
        {
            if (!User.IsStaff())
            {
                var fromAccount = await _accountService.GetAccountByIdAsync(dto.FromAccountId);
                if (fromAccount is null)
                    return NotFound(new { message = "Source account not found." });

                if (!User.CanAccessUser(fromAccount.UserId))
                    return Forbid();
            }
            var transaction = await _transactionService.TransferAsync(dto);

            return Ok(transaction);
        }

        [HttpGet("account/{accountId:long}")]
        public async Task<ActionResult<PagedResultDto<TransactionResponseDto>>> GetByAccountId(
            long accountId,
            [FromQuery] string? filter = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (filter == "daterange" && (!from.HasValue || !to.HasValue))
                return BadRequest(new { message = "Both 'from' and 'to' dates are required for date range filter." });
            if (!User.IsStaff())
            {
                var account = await _accountService.GetAccountByIdAsync(accountId);
                if (account is null)
                    return NotFound(new { message = "Account not found." });

                if (!User.CanAccessUser(account.UserId))
                    return Forbid();
            }

            var transactions = await _transactionService.GetTransactionsByAccountIdAsync(
                accountId, filter, from, to, pageNumber, pageSize);
            return Ok(transactions);
        }

        [HttpGet("account/{accountId:long}/summary")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<ActionResult<TransactionSummaryDto>> GetTransactionSummary(long accountId)
        {
            var summary = await _transactionService.GetTransactionSummaryByAccountIdAsync(accountId);
            return Ok(summary);
        }
    }
}
