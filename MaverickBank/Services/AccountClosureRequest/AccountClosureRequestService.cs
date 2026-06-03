using MaverickBank.Data;
using MaverickBank.DTOs.AccountClosureRequest;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.AccountClosureRequest
{
    public class AccountClosureRequestService : IAccountClosureRequestService
    {
        private readonly AppDbContext _context;

        public AccountClosureRequestService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AccountClosureRequestResponseDto> CreateRequestAsync(CreateAccountClosureRequestDto dto)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == dto.AccountId);

            if (account is null)
                throw new Exception("Account not found");

            if (account.UserId != dto.RequestedBy)
                throw new Exception("You can only close your own account");

            if (account.Balance > 0)
                throw new Exception("Account balance must be zero before closure");

            var pendingRequest =
                await _context.AccountClosureRequests
                    .AnyAsync(r => r.AccountId == dto.AccountId && r.Status == "Pending");

            if (pendingRequest)
                throw new Exception("A pending closure request already exists");

            var request = new Models.AccountClosureRequest
            {
                AccountId = dto.AccountId,
                RequestedBy = dto.RequestedBy,
                RequestDate = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.AccountClosureRequests.Add(request);

            await _context.SaveChangesAsync();

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

        public async Task<IEnumerable<AccountClosureRequestResponseDto>> GetPendingRequestsAsync()
        {
            return await _context.AccountClosureRequests
                .Where(r => r.Status == "Pending")
                .OrderBy(r => r.RequestDate)
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
        }

        public async Task<bool> ApproveRequestAsync(long requestId, int reviewedBy, string remarks)
        {
            var request = await _context.AccountClosureRequests.FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request is null)
                return false;

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

            return true;
        }

    }
}
