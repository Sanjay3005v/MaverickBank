using MaverickBank.DTOs.AccountClosureRequest;
using MaverickBank.DTOs.Pagination;

namespace MaverickBank.Services.AccountClosureRequest
{
    public interface IAccountClosureRequestService
    {
        Task<AccountClosureRequestResponseDto> CreateRequestAsync(CreateAccountClosureRequestDto dto);
        Task<PagedResultDto<AccountClosureRequestResponseDto>> GetPendingRequestsAsync(int pageNumber, int pageSize);
        Task<bool> ApproveRequestAsync(long requestId, int reviewedBy, string remarks);
    }
}
