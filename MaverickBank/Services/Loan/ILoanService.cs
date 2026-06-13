using MaverickBank.DTOs.Loan;
using MaverickBank.DTOs.Pagination;
using MaverickBank.DTOs.Transaction;

namespace MaverickBank.Services.Loan
{
    public interface ILoanService
    {
        Task<LoanResponseDto> ApplyLoanAsync(ApplyLoanDto dto);
        Task<PagedResultDto<LoanResponseDto?>> GetLoansByUserIdAsync(int loanId, int pageNumber, int pageSize);
        Task<bool> UpdateLoanStatusAsync(int loanId, ApproveLoanDto dto);
        Task<bool> RepayLoanAsync(LoanRepaymentDto dto);
        Task<bool> RejectLoanAsync(int loanApplicationId, RejectLoanDto dto);
        Task<PagedResultDto<LoanResponseDto>> GetPendingLoanApplicationsAsync(int pageNumber, int pageSize);
        Task<LoanResponseDto?> GetLoanByIdAsync(int loanId);
        Task<int?> GetLoanOwnerUserIdAsync(int loanId);
    }
}
