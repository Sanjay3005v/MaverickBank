using MaverickBank.Data;
using MaverickBank.DTOs.Account;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.Account
{
    public class AccountService : IAccountService
    {
        private readonly AppDbContext _context;

        public AccountService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AccountResponseDto>> GetAllAccountsAsync()
        {
            return await _context.Accounts
                .Select(a => new AccountResponseDto(
                    a.AccountId,
                    a.AccountNumber,
                    a.Balance,
                    a.Status,
                    a.OpenedDate,
                    a.ClosedDate,
                    a.UserId,
                    a.BranchId,
                    a.AccountTypeId
                )).ToListAsync();
        }

        public async Task<AccountResponseDto?> GetAccountByIdAsync(long accountId)
        {
            return await _context.Accounts
                .Where(a => a.AccountId == accountId)
                .Select(a => new AccountResponseDto(
                    a.AccountId,
                    a.AccountNumber,
                    a.Balance,
                    a.Status,
                    a.OpenedDate,
                    a.ClosedDate,
                    a.UserId,
                    a.BranchId,
                    a.AccountTypeId
                )).FirstOrDefaultAsync();
        }

        public async Task<AccountResponseDto> CreateAccountAsync(CreateAccountDto dto)
        {
            var account = new Models.Account
            {
                UserId = dto.UserId,
                BranchId = dto.BranchId,
                AccountTypeId = dto.AccountTypeId,
                Balance = dto.InitialDeposit,
                Status = "Active",
                OpenedDate = DateTime.UtcNow,
                AccountNumber = await GenerateAccountNumber()
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return new AccountResponseDto(
                account.AccountId,
                account.AccountNumber,
                account.Balance,
                account.Status,
                account.OpenedDate,
                account.ClosedDate,
                account.UserId,
                account.BranchId,
                account.AccountTypeId
            );
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
