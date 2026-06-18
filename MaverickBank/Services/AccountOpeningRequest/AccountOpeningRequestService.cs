using MaverickBank.Data;
using MaverickBank.Models;
using MaverickBank.DTOs.Account;
using MaverickBank.DTOs.AccountOpeningRequestDto;
using MaverickBank.DTOs.Pagination;
using MaverickBank.Services.Account;
using MaverickBank.Services.AuditLog;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.AccountOpeningRequest
{
    public class AccountOpeningRequestService : IAccountOpeningRequestService
    {
        private readonly AppDbContext _context;
        private readonly IAccountService _accountService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AccountOpeningRequestService> _logger;

        public AccountOpeningRequestService(
            AppDbContext context,
            IAccountService accountService,
            IAuditLogService auditLogService,
            ILogger<AccountOpeningRequestService> logger)
        {
            _context = context;
            _accountService = accountService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<AccountOpeningRequestResponseDto> CreateRequestAsync(CreateAccountOpeningRequestDto dto)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == dto.UserId))
                throw new KeyNotFoundException($"User with ID {dto.UserId} not found.");

            if (!await _context.Branches.AnyAsync(b => b.BranchId == dto.BranchId))
                throw new KeyNotFoundException($"Branch with ID {dto.BranchId} not found.");

            if (!await _context.AccountTypes.AnyAsync(t => t.AccountTypeId == dto.AccountTypeId))
                throw new KeyNotFoundException($"Account type with ID {dto.AccountTypeId} not found.");

            if (dto.InitialDeposit < 0)
                throw new InvalidOperationException("Initial deposit cannot be negative.");

            if (await _context.AccountOpeningRequests.AnyAsync(r =>
                    r.UserId == dto.UserId && r.BranchId == dto.BranchId &&
                    r.AccountTypeId == dto.AccountTypeId && r.Status == "Pending"))
                throw new InvalidOperationException("A pending request already exists for this branch and account type.");

            var request = new Models.AccountOpeningRequest
            {
                UserId = dto.UserId,
                BranchId = dto.BranchId,
                AccountTypeId = dto.AccountTypeId,
                InitialDeposit = dto.InitialDeposit,
                RequestDate = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.AccountOpeningRequests.Add(request);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(dto.UserId, "Account Opening Requested", "AccountOpeningRequest", request.RequestId);
            _logger.LogInformation("Account opening request {RequestId} created for user {UserId}", request.RequestId, dto.UserId);

            return ToDto(request);
        }

        public async Task<PagedResultDto<AccountOpeningRequestResponseDto>> GetPendingRequestsAsync(int pageNumber, int pageSize)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var query = _context.AccountOpeningRequests
                .Where(r => r.Status == "Pending")
                .OrderBy(r => r.RequestDate);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResultDto<AccountOpeningRequestResponseDto>(
                items.Select(ToDto), pageNumber, pageSize, totalCount,
                (int)Math.Ceiling(totalCount / (double)pageSize));
        }

        public async Task<PagedResultDto<AccountOpeningRequestResponseDto>> GetRequestsByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var query = _context.AccountOpeningRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RequestDate);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResultDto<AccountOpeningRequestResponseDto>(
                items.Select(ToDto), pageNumber, pageSize, totalCount,
                (int)Math.Ceiling(totalCount / (double)pageSize));
        }

        public async Task<bool> ApproveRequestAsync(long requestId, int reviewedBy, string remarks)
        {
            var request = await _context.AccountOpeningRequests.FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request is null)
                return false;

            if (request.Status != "Pending")
                throw new InvalidOperationException("Only pending requests can be approved.");

            var account = await _accountService.CreateAccountAsync(
                new CreateAccountDto(request.UserId, request.BranchId, request.AccountTypeId, request.InitialDeposit));

            request.Status = "Approved";
            request.ReviewedBy = reviewedBy;
            request.ReviewedDate = DateTime.UtcNow;
            request.Remarks = remarks;
            request.CreatedAccountId = account.AccountId;

            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(reviewedBy, "Account Opening Approved", "AccountOpeningRequest", requestId);
            _logger.LogInformation("Account opening request {RequestId} approved by {ReviewedBy}, account {AccountId} created",
                requestId, reviewedBy, account.AccountId);

            return true;
        }

        public async Task<bool> RejectRequestAsync(long requestId, int reviewedBy, string remarks)
        {
            var request = await _context.AccountOpeningRequests.FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request is null)
                return false;

            if (request.Status != "Pending")
                throw new InvalidOperationException("Only pending requests can be rejected.");

            request.Status = "Rejected";
            request.ReviewedBy = reviewedBy;
            request.ReviewedDate = DateTime.UtcNow;
            request.Remarks = remarks;

            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(reviewedBy, "Account Opening Rejected", "AccountOpeningRequest", requestId);
            _logger.LogInformation("Account opening request {RequestId} rejected by {ReviewedBy}", requestId, reviewedBy);

            return true;
        }

        private static AccountOpeningRequestResponseDto ToDto(Models.AccountOpeningRequest r) =>
            new(r.RequestId, r.UserId, r.BranchId, r.AccountTypeId, r.InitialDeposit,
                r.RequestDate, r.Status, r.ReviewedBy, r.ReviewedDate, r.Remarks, r.CreatedAccountId);
    }
}
