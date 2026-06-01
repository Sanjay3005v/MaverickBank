using MaverickBank.DTOs.Loan;

namespace MaverickBank.Services.Loan
{
    public interface ILoanService
    {
        Task<LoanResponseDto> ApplyLoanAsync(ApplyLoanDto dto);

        Task<IEnumerable<LoanResponseDto>> GetLoansByUserIdAsync(int userId);

        Task<LoanResponseDto?> GetLoanByIdAsync(int loanId);

        Task<bool> UpdateLoanStatusAsync(int loanId, ApproveLoanDto dto);
    }
}
