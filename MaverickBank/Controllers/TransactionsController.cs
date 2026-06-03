using MaverickBank.DTOs.Transaction;
using MaverickBank.Services.Transaction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MaverickBank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost("deposit")]
        [Authorize(Roles = "Customer,Employee")]
        public async Task<ActionResult<TransactionResponseDto>> Deposit(DepositDto dto)
        {
            var transaction = await _transactionService.DepositAsync(dto);

            return Ok(transaction);
        }

        [HttpPost("withdraw")]
        [Authorize(Roles = "Customer,Employee")]
        public async Task<ActionResult<TransactionResponseDto>> Withdraw(WithdrawDto dto)
        {
            var transaction = await _transactionService.WithdrawAsync(dto);

            return Ok(transaction);
        }


        [HttpPost("transfer")]
        [Authorize(Roles = "Customer,Employee")]
        public async Task<ActionResult<TransactionResponseDto>> Transfer(TransferDto dto)
        {
            var transaction = await _transactionService.TransferAsync(dto);

            return Ok(transaction);
        }

        [HttpGet("account/{accountId:long}")]
        public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetByAccountId(long accountId)
        {
            var transactions = await _transactionService.GetTransactionsByAccountIdAsync(accountId);

            return Ok(transactions);
        }
    }
}
