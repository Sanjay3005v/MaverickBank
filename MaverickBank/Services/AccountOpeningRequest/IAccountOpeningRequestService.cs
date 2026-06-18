using MaverickBank.DTOs.AccountOpeningRequestDto;
using MaverickBank.DTOs.Pagination;

namespace MaverickBank.Services.AccountOpeningRequest
{
    public interface IAccountOpeningRequestService
    {
        Task<AccountOpeningRequestResponseDto> CreateRequestAsync(CreateAccountOpeningRequestDto dto);
        Task<PagedResultDto<AccountOpeningRequestResponseDto>> GetPendingRequestsAsync(int pageNumber, int pageSize);
        Task<PagedResultDto<AccountOpeningRequestResponseDto>> GetRequestsByUserIdAsync(int userId, int pageNumber, int pageSize);
        Task<bool> ApproveRequestAsync(long requestId, int reviewedBy, string remarks);
        Task<bool> RejectRequestAsync(long requestId, int reviewedBy, string remarks);
    }
}
