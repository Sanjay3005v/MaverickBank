using Asp.Versioning;
using MaverickBank.DTOs.Account;
using MaverickBank.Services.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaverickBank.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<ActionResult<IEnumerable<AccountResponseDto>>> GetAllAccounts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var accounts = await _accountService.GetAllAccountsAsync(pageNumber, pageSize);
            return Ok(accounts);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<AccountResponseDto>> GetAccountById(long id)
        {
            var account = await _accountService.GetAccountByIdAsync(id);

            if (account is null)
                return NotFound(new { message = $"Account with ID {id} not found." });

            return Ok(account);
        }

        [HttpPost]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<ActionResult<AccountResponseDto>> CreateAccount(CreateAccountDto dto)
        {
            var createdAccount = await _accountService.CreateAccountAsync(dto);

            return CreatedAtAction(nameof(GetAccountById),
                new { id = createdAccount.AccountId }, createdAccount);
        }

        [HttpPut("{id:long}/status")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> UpdateAccountStatus(long id, UpdateAccountDto dto)
        {
            var updated = await _accountService.UpdateAccountStatusAsync(id, dto);

            if (!updated)
                return NotFound(new { message = $"Account with ID {id} not found." });

            return NoContent();
        }

        [HttpPut("{id:long}/close")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> CloseAccount(long id, CloseAccountDto dto)
        {
            var closed = await _accountService.CloseAccountAsync(id, dto);

            if (!closed)
                return NotFound(new { message = $"Account with ID {id} not found." });

            return NoContent();
        }

        [HttpGet("user/{userId:int}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<AccountResponseDto>>> GetAccountsByUserId(int userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var accounts = await _accountService.GetAccountsByUserIdAsync(userId, pageNumber, pageSize);
            return Ok(accounts);
        }
    }
}
