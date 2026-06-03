using MaverickBank.DTOs.AccountClosureRequest;

namespace MaverickBank.Services.AccountClosureRequest
{
    public interface IAccountClosureRequestService
    {
        Task<AccountClosureRequestResponseDto> CreateRequestAsync(CreateAccountClosureRequestDto dto);

        Task<IEnumerable<AccountClosureRequestResponseDto>> GetPendingRequestsAsync();

        Task<bool> ApproveRequestAsync(long requestId, int reviewedBy, string remarks);
    }
}
