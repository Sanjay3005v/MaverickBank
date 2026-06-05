using MaverickBank.DTOs.Beneficiary;
using MaverickBank.DTOs.Pagination;

namespace MaverickBank.Services.Beneficiary
{
    public interface IBeneficiaryService
    {
        Task<BeneficiaryResponseDto> AddBeneficiaryAsync(AddBeneficiaryDto dto);
        Task<PagedResultDto<BeneficiaryResponseDto>> GetBeneficiariesByUserIdAsync(long userId, int pageNumber, int pageSize);
        Task<bool> DeleteBeneficiaryAsync(long beneficiaryId);
    }
}
