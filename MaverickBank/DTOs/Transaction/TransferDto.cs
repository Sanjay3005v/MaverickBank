namespace MaverickBank.DTOs.Transaction
{
    public record TransferDto(
        long FromAccountId,
        long ToAccountId,
        decimal Amount,
        string Description
    );
}
