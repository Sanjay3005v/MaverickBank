using MaverickBank.DTOs.Pagination;
using MaverickBank.DTOs.Transaction;

namespace MaverickBank.Services.Transaction
{
    public interface ITransactionService
    {
        Task<TransactionResponseDto> DepositAsync(DepositDto dto);
        Task<TransactionResponseDto> WithdrawAsync(WithdrawDto dto);
        Task<TransactionResponseDto> TransferAsync(TransferDto dto);
        Task<PagedResultDto<TransactionResponseDto>> GetTransactionsByAccountIdAsync(
            long accountId,
            string? filter = null,
            DateTime? from = null,
            DateTime? to = null,
            int pageNumber = 1,
            int pageSize = 10);
        Task<TransactionSummaryDto> GetTransactionSummaryByAccountIdAsync(long accountId);

    }
}
