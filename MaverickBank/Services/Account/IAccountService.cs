using MaverickBank.DTOs.Account;


namespace MaverickBank.Services.Account
{
    public interface IAccountService
    {
        Task<IEnumerable<AccountResponseDto>> GetAllAccountsAsync();

        Task<AccountResponseDto?> GetAccountByIdAsync(long accountId);

        Task<AccountResponseDto> CreateAccountAsync(CreateAccountDto dto);

        Task<bool> UpdateAccountStatusAsync(long accountId,UpdateAccountDto dto);

        Task<bool> CloseAccountAsync(long accountId,CloseAccountDto dto);
        Task<IEnumerable<AccountResponseDto>> GetAccountsByUserIdAsync(int userId);
    }
}
