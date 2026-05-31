namespace MaverickBank.DTOs.Beneficiary
{
    public record AddBeneficiaryDto(
        int UserId,
        string BeneficiaryName,
        string AccountNumber,
        string BankName,
        string BranchName,
        string IFSCCode
    );
}
