using MaverickBank.DTOs.Loan;
using MaverickBank.DTOs.Pagination;

namespace MaverickBank.Services.Loan
{
    public interface ILoanTypeService
    {
        Task<PagedResultDto<LoanTypeResponseDto>> GetAllLoanTypesAsync(int pageNumber, int pageSize);
        Task<LoanTypeResponseDto?> GetLoanTypeByIdAsync(int loanTypeId);
        Task<LoanTypeResponseDto> CreateLoanTypeAsync(CreateLoanTypeDto dto);
        Task<bool> UpdateLoanTypeAsync(int loanTypeId, CreateLoanTypeDto dto);
        Task<bool> DeleteLoanTypeAsync(int loanTypeId);
    }
}
