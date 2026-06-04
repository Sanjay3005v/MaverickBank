using MaverickBank.DTOs.Loan;

namespace MaverickBank.Services.Loan
{
    public interface ILoanTypeService
    {
        Task<IEnumerable<LoanTypeResponseDto>> GetAllLoanTypesAsync();
        Task<LoanTypeResponseDto?> GetLoanTypeByIdAsync(int loanTypeId);
        Task<LoanTypeResponseDto> CreateLoanTypeAsync(CreateLoanTypeDto dto);
        Task<bool> UpdateLoanTypeAsync(int loanTypeId, CreateLoanTypeDto dto);
        Task<bool> DeleteLoanTypeAsync(int loanTypeId);
    }
}
