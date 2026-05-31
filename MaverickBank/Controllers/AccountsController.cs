using MaverickBank.DTOs.Account;
using MaverickBank.Services.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MaverickBank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountResponseDto>>> GetAllAccounts()
        {
            var accounts = await _accountService.GetAllAccountsAsync();
            return Ok(accounts);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<AccountResponseDto>> GetAccountById(long id)
        {
            var account = await _accountService.GetAccountByIdAsync(id);

            if (account is null)
                return NotFound();

            return Ok(account);
        }

        [HttpPost]
        public async Task<ActionResult<AccountResponseDto>> CreateAccount(CreateAccountDto dto)
        {
            var createdAccount = await _accountService.CreateAccountAsync(dto);

            return CreatedAtAction(nameof(GetAccountById),
                new { id = createdAccount.AccountId },
                createdAccount);
        }

        [HttpPut("{id:long}/status")]
        public async Task<IActionResult> UpdateAccountStatus(long id, UpdateAccountDto dto)
        {
            var updated = await _accountService.UpdateAccountStatusAsync(id, dto);

            if (!updated)
                return NotFound();

            return NoContent();
        }

        [HttpPut("{id:long}/close")]
        public async Task<IActionResult> CloseAccount(long id, CloseAccountDto dto)
        {
            var closed = await _accountService.CloseAccountAsync(id, dto);

            if (!closed)
                return NotFound();

            return NoContent();
        }
    }
}
