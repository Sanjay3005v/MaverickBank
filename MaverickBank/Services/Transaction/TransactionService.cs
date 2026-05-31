using MaverickBank.Data;
using MaverickBank.DTOs.Transaction;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.Transaction
{
    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _context;

        public TransactionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TransactionResponseDto> DepositAsync(DepositDto dto)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == dto.AccountId);

            if (account is null)
                throw new Exception("Account not found");

            account.Balance += dto.Amount;

            var transaction = new Models.Transaction
            {
                TransactionTypeId = 1,
                ToAccountId = dto.AccountId,
                Amount = dto.Amount,
                Description = dto.Description,
                TransactionReference = GenerateTransactionReference(),
                TransactionStatus = "Success",
                TransactionDate = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            return MapToResponse(transaction);
        }

        public async Task<TransactionResponseDto> WithdrawAsync(WithdrawDto dto)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == dto.AccountId);

            if (account is null)
                throw new Exception("Account not found");

            if (account.Balance < dto.Amount)
                throw new Exception("Insufficient balance");

            account.Balance -= dto.Amount;

            var transaction = new Models.Transaction
            {
                TransactionTypeId = 2,
                FromAccountId = dto.AccountId,
                Amount = dto.Amount,
                Description = dto.Description,
                TransactionReference = GenerateTransactionReference(),
                TransactionStatus = "Success",
                TransactionDate = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            return MapToResponse(transaction);
        }

        public async Task<TransactionResponseDto> TransferAsync(TransferDto dto)
        {
            var fromAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == dto.FromAccountId);

            var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == dto.ToAccountId);

            if (fromAccount is null || toAccount is null)
                throw new Exception("Invalid account");

            if (fromAccount.Balance < dto.Amount)
                throw new Exception("Insufficient balance");

            fromAccount.Balance -= dto.Amount;
            toAccount.Balance += dto.Amount;

            var transaction = new Models.Transaction
            {
                TransactionTypeId = 3,
                FromAccountId = dto.FromAccountId,
                ToAccountId = dto.ToAccountId,
                Amount = dto.Amount,
                Description = dto.Description,
                TransactionReference = GenerateTransactionReference(),
                TransactionStatus = "Success",
                TransactionDate = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            return MapToResponse(transaction);
        }

        public async Task<IEnumerable<TransactionResponseDto>> GetTransactionsByAccountIdAsync(long accountId)
        {
            return await _context.Transactions
                .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new TransactionResponseDto(
                    t.TransactionId,
                    t.TransactionTypeId,
                    t.FromAccountId,
                    t.ToAccountId,
                    t.Amount,
                    t.TransactionReference,
                    t.Description,
                    t.TransactionStatus,
                    t.TransactionDate
                )).ToListAsync();
        }

        private string GenerateTransactionReference()
        {
            return $"TXN{DateTime.UtcNow.Ticks}";
        }

        private TransactionResponseDto MapToResponse(Models.Transaction transaction)
        {
            return new TransactionResponseDto(
                transaction.TransactionId,
                transaction.TransactionTypeId,
                transaction.FromAccountId,
                transaction.ToAccountId,
                transaction.Amount,
                transaction.TransactionReference,
                transaction.Description,
                transaction.TransactionStatus,
                transaction.TransactionDate
            );
        }
    }
}
