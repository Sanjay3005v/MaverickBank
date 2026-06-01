using MaverickBank.DTOs.Beneficiary;

namespace MaverickBank.Services.Beneficiary
{
    public interface IBeneficiaryService
    {
        Task<BeneficiaryResponseDto> AddBeneficiaryAsync(AddBeneficiaryDto dto);

        Task<IEnumerable<BeneficiaryResponseDto>> GetBeneficiariesByUserIdAsync(long userId);

        Task<bool> DeleteBeneficiaryAsync(long beneficiaryId);
    }
}
