namespace MaverickBank.DTOs.Transaction
{
    public record TransactionResponseDto(
        long TransactionId,
        int TransactionTypeId,
        long? FromAccountId,
        long? ToAccountId,
        decimal Amount,
        string TransactionReference,
        string? Description,
        string TransactionStatus,
        DateTime TransactionDate
    );
}
