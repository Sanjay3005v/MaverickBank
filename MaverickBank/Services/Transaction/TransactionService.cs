using AutoMapper;
using MaverickBank.Data;
using MaverickBank.DTOs.Pagination;
using MaverickBank.DTOs.Transaction;
using MaverickBank.Models;
using MaverickBank.Services.AuditLog;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MaverickBank.Services.Transaction
{
    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(AppDbContext context, IMapper mapper, IAuditLogService auditLogService, ILogger<TransactionService> logger)
        {
            _context = context;
            _mapper = mapper;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<TransactionResponseDto> DepositAsync(DepositDto dto)
        {
            if (dto.Amount <= 0)
                throw new InvalidOperationException("Deposit amount must be greater than zero.");

            var account = await _context.Accounts.FindAsync(dto.AccountId)
                ?? throw new KeyNotFoundException("Account not found.");

            if (account.Status != "Active")
                throw new InvalidOperationException("Deposits can only be made to active accounts.");

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

            var resultDto = _mapper.Map<TransactionResponseDto>(transaction);

            await _auditLogService.LogAsync(account.UserId, "Deposit", "Transaction", transaction.TransactionId, newValues: JsonSerializer.Serialize(resultDto));

            _logger.LogInformation("Deposit of {Amount} to account {AccountId}", dto.Amount, dto.AccountId);

            return resultDto;
        }

        public async Task<TransactionResponseDto> WithdrawAsync(WithdrawDto dto)
        {
            if (dto.Amount <= 0)
                throw new InvalidOperationException("Withdrawal amount must be greater than zero.");

            var account = await _context.Accounts.FindAsync(dto.AccountId)
                ?? throw new KeyNotFoundException("Account not found.");

            if (account.Status != "Active")
                throw new InvalidOperationException("Withdrawals can only be made from active accounts.");

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
            var resultDto = _mapper.Map<TransactionResponseDto>(transaction);

            await _auditLogService.LogAsync(account.UserId, "Withdrawal", "Transaction", transaction.TransactionId, newValues: JsonSerializer.Serialize(resultDto));

            _logger.LogInformation("Withdrawal of {Amount} from account {AccountId}", dto.Amount, dto.AccountId);
            return resultDto;
        }

        public async Task<TransactionResponseDto> TransferAsync(TransferDto dto)
        {
            if (dto.Amount <= 0)
                throw new InvalidOperationException("Transfer amount must be greater than zero.");

            if (dto.FromAccountId == dto.ToAccountId)
                throw new InvalidOperationException("Source and destination accounts must be different.");

            var fromAccount = await _context.Accounts.FindAsync(dto.FromAccountId)
                ?? throw new KeyNotFoundException("Source account not found.");

            var toAccount = await _context.Accounts.FindAsync(dto.ToAccountId)
                ?? throw new KeyNotFoundException("Destination account not found.");

            if (fromAccount.Status != "Active")
                throw new InvalidOperationException("Source account is not active.");

            if (toAccount.Status != "Active")
                throw new InvalidOperationException("Destination account is not active.");

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
            var resultDto = _mapper.Map<TransactionResponseDto>(transaction);

            await _auditLogService.LogAsync(fromAccount.UserId, "Transfer", "Transaction", transaction.TransactionId, newValues: JsonSerializer.Serialize(resultDto));

            _logger.LogInformation("Transfer of {Amount} from {From} to {To}", dto.Amount, dto.FromAccountId, dto.ToAccountId);
            return resultDto;
        }

        public async Task<PagedResultDto<TransactionResponseDto>> GetTransactionsByAccountIdAsync(long accountId, string? filter = null, DateTime? from = null, DateTime? to = null, int pageNumber = 1, int pageSize = 10)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var query = _context.Transactions
                .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
                .OrderByDescending(t => t.TransactionDate)
                .AsQueryable();

            if (filter == "last10")
            {
                var last10 = await query.Take(10).ToListAsync();
                var last10Data = _mapper.Map<IEnumerable<TransactionResponseDto>>(last10);
                return new PagedResultDto<TransactionResponseDto>(last10Data, 1, 10, last10.Count, 1);
            }

            if (filter == "lastmonth")
            {
                var start = DateTime.UtcNow.AddMonths(-1);
                query = query.Where(t => t.TransactionDate >= start);
            }
            else if (filter == "daterange" && from.HasValue && to.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= from.Value && t.TransactionDate <= to.Value);
            }

            var totalCount = await query.CountAsync();
            var transactions = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = _mapper.Map<IEnumerable<TransactionResponseDto>>(transactions);
            return new PagedResultDto<TransactionResponseDto>(
                data, pageNumber, pageSize, totalCount,
                (int)Math.Ceiling(totalCount / (double)pageSize));
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
