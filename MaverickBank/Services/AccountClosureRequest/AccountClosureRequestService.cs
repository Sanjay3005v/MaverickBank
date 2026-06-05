using AutoMapper;
using MaverickBank.Data;
using MaverickBank.DTOs.AccountClosureRequest;
using MaverickBank.DTOs.Pagination;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.AccountClosureRequest
{
    public class AccountClosureRequestService : IAccountClosureRequestService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AccountClosureRequestService> _logger;

        public AccountClosureRequestService(AppDbContext context, ILogger<AccountClosureRequestService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AccountClosureRequestResponseDto> CreateRequestAsync(CreateAccountClosureRequestDto dto)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == dto.AccountId)
                            ?? throw new KeyNotFoundException("Account not found.");

            if (account.UserId != dto.RequestedBy)
                throw new InvalidOperationException("You can only close your own account.");

            if (account.Status == "Closed")
                throw new InvalidOperationException("Account is already closed.");

            if (account.Balance > 0)
                throw new InvalidOperationException("Account balance must be zero before closure.");

            if (await _context.AccountClosureRequests.AnyAsync(r => r.AccountId == dto.AccountId && r.Status == "Pending"))
                throw new InvalidOperationException("A pending closure request already exists for this account.");

            var request = new Models.AccountClosureRequest
            {
                AccountId = dto.AccountId,
                RequestedBy = dto.RequestedBy,
                RequestDate = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.AccountClosureRequests.Add(request);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Closure request {RequestId} created for account {AccountId}", request.RequestId, dto.AccountId);

            return new AccountClosureRequestResponseDto(
                request.RequestId,
                request.AccountId,
                request.RequestedBy,
                request.RequestDate,
                request.Status,
                request.ReviewedBy,
                request.ReviewedDate,
                request.Remarks
            );
        }

        public async Task<PagedResultDto<AccountClosureRequestResponseDto>> GetPendingRequestsAsync(int pageNumber, int pageSize)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var query = _context.AccountClosureRequests
                .Where(r => r.Status == "Pending")
                .OrderBy(r => r.RequestDate);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new AccountClosureRequestResponseDto(
                    r.RequestId,
                    r.AccountId,
                    r.RequestedBy,
                    r.RequestDate,
                    r.Status,
                    r.ReviewedBy,
                    r.ReviewedDate,
                    r.Remarks
                )).ToListAsync();

            return new PagedResultDto<AccountClosureRequestResponseDto>(
                items, pageNumber, pageSize, totalCount,
                (int)Math.Ceiling(totalCount / (double)pageSize));
        }
        public async Task<bool> ApproveRequestAsync(long requestId, int reviewedBy, string remarks)
        {
            var request = await _context.AccountClosureRequests.FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request is null)
                return false;

            if (request.Status != "Pending")
                throw new InvalidOperationException("Only pending requests can be approved.");

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == request.AccountId);

            if (account is null)
                throw new Exception("Account not found");

            request.Status = "Approved";
            request.ReviewedBy = reviewedBy;
            request.ReviewedDate = DateTime.UtcNow;
            request.Remarks = remarks;

            account.Status = "Closed";
            account.ClosedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Closure request {RequestId} approved by {ReviewedBy}", requestId, reviewedBy);

            return true;
        }

    }
}
