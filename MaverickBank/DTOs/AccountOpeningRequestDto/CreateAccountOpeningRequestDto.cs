namespace MaverickBank.DTOs.AccountOpeningRequestDto
{
    public record CreateAccountOpeningRequestDto(
        int UserId,
        int BranchId,
        int AccountTypeId,
        decimal InitialDeposit
    );
}
