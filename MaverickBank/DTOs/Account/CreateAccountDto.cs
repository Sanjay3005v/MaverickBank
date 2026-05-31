namespace MaverickBank.DTOs.Account
{
    public record CreateAccountDto(
        int UserId,
        int BranchId,
        int AccountTypeId,
        decimal InitialDeposit
    );
}
