using AutoMapper;
using MaverickBank.Data;
using MaverickBank.DTOs.Account;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.Account
{
    public class AccountService : IAccountService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<AccountService> _logger;

        public AccountService(AppDbContext context, IMapper mapper, ILogger<AccountService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<AccountResponseDto>> GetAllAccountsAsync()
        {
            var accounts = await _context.Accounts.ToListAsync();
            return _mapper.Map<IEnumerable<AccountResponseDto>>(accounts);
        }

        public async Task<AccountResponseDto?> GetAccountByIdAsync(long accountId)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            return account is null ? null : _mapper.Map<AccountResponseDto>(account);
        }

        public async Task<AccountResponseDto> CreateAccountAsync(CreateAccountDto dto)
        {
            var account = _mapper.Map<Models.Account>(dto);
            account.Status = "Active";
            account.AccountNumber = await GenerateAccountNumber();

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created account {AccountId} for user {UserId}", account.AccountId, account.UserId);
            return _mapper.Map<AccountResponseDto>(account);
        }

        public async Task<bool> UpdateAccountStatusAsync(long accountId, UpdateAccountDto dto)
        {
            var account = await _context.Accounts.FindAsync(accountId);

            if (account is null)
                return false;

            account.Status = dto.Status;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CloseAccountAsync(long accountId, CloseAccountDto dto)
        {
            var account = await _context.Accounts.FindAsync(accountId);

            if (account is null)
                return false;

            account.Status = "Closed";
            account.ClosedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        private async Task<string> GenerateAccountNumber()
        {
            string accountNumber;

            do
            {
                accountNumber = $"{Random.Shared.Next(1000, 9999)}" + $"{Random.Shared.Next(1000, 9999)}" + $"{Random.Shared.Next(1000, 9999)}";

            } while (await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber));

            return accountNumber;
        }
    }
}
