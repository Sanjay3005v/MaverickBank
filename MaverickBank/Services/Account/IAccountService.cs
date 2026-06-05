using MaverickBank.DTOs.Account;
using MaverickBank.DTOs.Pagination;


namespace MaverickBank.Services.Account
{
    public interface IAccountService
    {
        Task<PagedResultDto<AccountResponseDto>> GetAllAccountsAsync(int pageNumber, int pageSize);

        Task<AccountResponseDto?> GetAccountByIdAsync(long accountId);

        Task<AccountResponseDto> CreateAccountAsync(CreateAccountDto dto);

        Task<bool> UpdateAccountStatusAsync(long accountId, UpdateAccountDto dto);

        Task<bool> CloseAccountAsync(long accountId, CloseAccountDto dto);

        Task<PagedResultDto<AccountResponseDto>> GetAccountsByUserIdAsync(int userId, int pageNumber, int pageSize);

    }
}
