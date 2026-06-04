using AutoMapper;
using MaverickBank.Data;
using MaverickBank.DTOs.Transaction;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.Transaction
{
    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(AppDbContext context, IMapper mapper, ILogger<TransactionService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TransactionResponseDto> DepositAsync(DepositDto dto)
        {
            var account = await _context.Accounts.FindAsync(dto.AccountId)
                ?? throw new KeyNotFoundException("Account not found.");

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

            _logger.LogInformation("Deposit of {Amount} to account {AccountId}", dto.Amount, dto.AccountId);
            return _mapper.Map<TransactionResponseDto>(transaction);
        }

        public async Task<TransactionResponseDto> WithdrawAsync(WithdrawDto dto)
        {
            var account = await _context.Accounts.FindAsync(dto.AccountId)
                ?? throw new KeyNotFoundException("Account not found.");

            if (account.Balance < dto.Amount)
                throw new InvalidOperationException("Insufficient balance.");

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

            _logger.LogInformation("Withdrawal of {Amount} from account {AccountId}", dto.Amount, dto.AccountId);
            return _mapper.Map<TransactionResponseDto>(transaction);
        }

        public async Task<TransactionResponseDto> TransferAsync(TransferDto dto)
        {
            var fromAccount = await _context.Accounts.FindAsync(dto.FromAccountId)
                ?? throw new KeyNotFoundException("Source account not found.");

            var toAccount = await _context.Accounts.FindAsync(dto.ToAccountId)
                ?? throw new KeyNotFoundException("Destination account not found.");

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

            _logger.LogInformation("Transfer of {Amount} from {From} to {To}", dto.Amount, dto.FromAccountId, dto.ToAccountId);
            return _mapper.Map<TransactionResponseDto>(transaction);
        }

        public async Task<IEnumerable<TransactionResponseDto>> GetTransactionsByAccountIdAsync(long accountId,string? filter = null,DateTime? from = null,DateTime? to = null)
        {
            var query = _context.Transactions
                .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
                .OrderByDescending(t => t.TransactionDate)
                .AsQueryable();

            if (filter == "last10")
            {
                query = query.Take(10);
            }
            else if (filter == "lastmonth")
            {
                var start = DateTime.UtcNow.AddMonths(-1);
                query = query.Where(t => t.TransactionDate >= start);
            }
            else if (filter == "daterange" && from.HasValue && to.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= from.Value && t.TransactionDate <= to.Value);
            }

            var transactions = await query.ToListAsync();
            return _mapper.Map<IEnumerable<TransactionResponseDto>>(transactions);
        }

        private string GenerateTransactionReference()
        {
            return $"TXN{DateTime.UtcNow.Ticks}";
        }

        public async Task<TransactionSummaryDto> GetTransactionSummaryByAccountIdAsync(long accountId)
        {
            var transactions = await _context.Transactions
                .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
                .ToListAsync();

            var totalInbound = transactions
                .Where(t => t.ToAccountId == accountId)
                .Sum(t => t.Amount);

            var totalOutbound = transactions
                .Where(t => t.FromAccountId == accountId)
                .Sum(t => t.Amount);

            var inboundCount = transactions.Count(t => t.ToAccountId == accountId);
            var outboundCount = transactions.Count(t => t.FromAccountId == accountId);

            return new TransactionSummaryDto(accountId, totalInbound, totalOutbound, inboundCount, outboundCount);
        }
    }
}
