using AutoMapper;
using MaverickBank.Data;
using MaverickBank.DTOs.Account;
using MaverickBank.DTOs.Pagination;
using MaverickBank.Services.AuditLog;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace MaverickBank.Services.Account
{
    public class AccountService : IAccountService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<AccountService> _logger;
        private readonly IAuditLogService _auditLogService;


        public AccountService(AppDbContext context, IMapper mapper, IAuditLogService auditLogService, ILogger<AccountService> logger)
        {
            _context = context;
            _mapper = mapper;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<PagedResultDto<AccountResponseDto>> GetAllAccountsAsync(int pageNumber, int pageSize)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var totalCount = await _context.Accounts.CountAsync();
            var data = await _context.Accounts
                .OrderBy(a => a.AccountId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Join(_context.Branches, a => a.BranchId, b => b.BranchId, (a, b) => new { a, b })
                .Join(_context.AccountTypes, x => x.a.AccountTypeId, t => t.AccountTypeId, (x, t) => new AccountResponseDto(
                    x.a.AccountId,
                    x.a.AccountNumber,
                    x.a.Balance,
                    x.a.Status,
                    x.a.OpenedDate,
                    x.a.ClosedDate,
                    x.a.UserId,
                    x.a.BranchId,
                    x.b.BranchName,
                    x.b.IFSCCode,
                    x.b.AddressLine1,
                    x.a.AccountTypeId,
                    t.TypeName
                ))
                .ToListAsync();
            return new PagedResultDto<AccountResponseDto>(
                data, pageNumber, pageSize, totalCount,
                (int)Math.Ceiling(totalCount / (double)pageSize));
        }

        public async Task<AccountResponseDto?> GetAccountByIdAsync(long accountId)
        {
            return await _context.Accounts
                .Where(a => a.AccountId == accountId)
                .Join(_context.Branches, a => a.BranchId, b => b.BranchId, (a, b) => new { a, b })
                .Join(_context.AccountTypes, x => x.a.AccountTypeId, t => t.AccountTypeId, (x, t) => new AccountResponseDto(
                    x.a.AccountId,
                    x.a.AccountNumber,
                    x.a.Balance,
                    x.a.Status,
                    x.a.OpenedDate,
                    x.a.ClosedDate,
                    x.a.UserId,
                    x.a.BranchId,
                    x.b.BranchName,
                    x.b.IFSCCode,
                    x.b.AddressLine1,
                    x.a.AccountTypeId,
                    t.TypeName
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<AccountResponseDto> CreateAccountAsync(CreateAccountDto dto)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == dto.UserId))
                throw new KeyNotFoundException($"User with ID {dto.UserId} not found.");

            if (!await _context.Branches.AnyAsync(b => b.BranchId == dto.BranchId))
                throw new KeyNotFoundException($"Branch with ID {dto.BranchId} not found.");

            if (!await _context.AccountTypes.AnyAsync(t => t.AccountTypeId == dto.AccountTypeId))
                throw new KeyNotFoundException($"Account type with ID {dto.AccountTypeId} not found.");

            if (dto.InitialDeposit < 0)
                throw new InvalidOperationException("Initial deposit cannot be negative.");

            var account = _mapper.Map<Models.Account>(dto);
            account.Status = "Active";
            account.AccountNumber = await GenerateAccountNumber();
            account.OpenedDate = DateTime.UtcNow;

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(dto.UserId, "Account Created", "Account", account.AccountId, newValues: JsonSerializer.Serialize(account));
            _logger.LogInformation("Created account {AccountId} for user {UserId}", account.AccountId, account.UserId);
            return await GetAccountByIdAsync(account.AccountId) ?? throw new Exception("Failed to retrieve created account.");
        }

        public async Task<bool> UpdateAccountStatusAsync(long accountId, UpdateAccountDto dto, int performedByUserId)
        {
            var account = await _context.Accounts.FindAsync(accountId);

            if (account is null)
                return false;

            account.Status = dto.Status;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(performedByUserId, $"Account Status Changed to {dto.Status}",
                "Account", accountId);

            _logger.LogInformation("Account {AccountId} status updated to {Status}", accountId, dto.Status);

            return true;
        }

        public async Task<bool> CloseAccountAsync(long accountId, CloseAccountDto dto, int performedByUserId)
        {
            var account = await _context.Accounts.FindAsync(accountId);

            if (account is null)
                return false;

            if (account.Status == "Closed")
                throw new InvalidOperationException("Account is already closed.");

            account.Status = "Closed";
            account.ClosedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(performedByUserId, "Account Closed", "Account", accountId);
            _logger.LogInformation("Account {AccountId} closed", accountId);

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

        public async Task<PagedResultDto<AccountResponseDto>> GetAccountsByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var totalCount = await _context.Accounts
                .Where(a => a.UserId == userId).CountAsync();
            var data = await _context.Accounts
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.AccountId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Join(_context.Branches, a => a.BranchId, b => b.BranchId, (a, b) => new { a, b })
                .Join(_context.AccountTypes, x => x.a.AccountTypeId, t => t.AccountTypeId, (x, t) => new AccountResponseDto(
                    x.a.AccountId,
                    x.a.AccountNumber,
                    x.a.Balance,
                    x.a.Status,
                    x.a.OpenedDate,
                    x.a.ClosedDate,
                    x.a.UserId,
                    x.a.BranchId,
                    x.b.BranchName,
                    x.b.IFSCCode,
                    x.b.AddressLine1,
                    x.a.AccountTypeId,
                    t.TypeName
                ))
                .ToListAsync();

            return new PagedResultDto<AccountResponseDto>(
                data, pageNumber, pageSize, totalCount,
                (int)Math.Ceiling(totalCount / (double)pageSize));
        }
    }
}
