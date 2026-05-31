namespace MaverickBank.DTOs.Beneficiary
{
    public record BeneficiaryResponseDto(
    int BeneficiaryId,
    int UserId,
    string BeneficiaryName,
    string AccountNumber,
    string BankName,
    string BranchName,
    string IFSCCode,
    DateTime CreatedAt
);
}
