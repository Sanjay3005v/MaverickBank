namespace MaverickBank.DTOs.Transaction
{
    public record TransactionSummaryDto(
        long AccountId,
        decimal TotalInbound,
        decimal TotalOutbound,
        int InboundCount,
        int OutboundCount
    );
}
