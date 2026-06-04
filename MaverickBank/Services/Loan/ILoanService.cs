using MaverickBank.DTOs.Loan;
using MaverickBank.DTOs.Transaction;

namespace MaverickBank.Services.Loan
{
    public interface ILoanService
    {
        Task<LoanResponseDto> ApplyLoanAsync(ApplyLoanDto dto);

        Task<IEnumerable<LoanResponseDto>> GetLoansByUserIdAsync(int userId);

        Task<LoanResponseDto?> GetLoanByIdAsync(int loanId);

        Task<bool> UpdateLoanStatusAsync(int loanId, ApproveLoanDto dto);

        Task<bool> RepayLoanAsync(LoanRepaymentDto dto);

        Task<bool> RejectLoanAsync(int loanApplicationId, RejectLoanDto dto);

        Task<IEnumerable<LoanResponseDto>> GetPendingLoanApplicationsAsync();
    }
}
