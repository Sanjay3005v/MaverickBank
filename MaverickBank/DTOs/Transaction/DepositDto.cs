namespace MaverickBank.DTOs.Transaction
{
    public record DepositDto(
        long AccountId,
        decimal Amount,
        string Description
    );
}
