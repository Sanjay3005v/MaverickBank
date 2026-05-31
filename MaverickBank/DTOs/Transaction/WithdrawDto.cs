namespace MaverickBank.DTOs.Transaction
{
    public record WithdrawDto(
        long AccountId,
        decimal Amount,
        string Description
    );
}
