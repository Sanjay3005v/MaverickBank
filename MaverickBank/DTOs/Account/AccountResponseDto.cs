namespace MaverickBank.DTOs.Account
{
    public record AccountResponseDto(
    long AccountId,
    string AccountNumber,
    decimal Balance,
    string Status,
    DateTime OpenedDate,
    DateTime? ClosedDate,
    int UserId,
    int BranchId,
    string BranchName,
    string IFSCCode,
    string BranchAddress,
    int AccountTypeId,
    string AccountTypeName
);
}
