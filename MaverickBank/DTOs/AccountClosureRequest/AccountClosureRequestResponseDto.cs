namespace MaverickBank.DTOs.AccountClosureRequest
{
    public record AccountClosureRequestResponseDto(
        long RequestId,
        long AccountId,
        int RequestedBy,
        DateTime RequestDate,
        string Status,
        int? ReviewedBy,
        DateTime? ReviewedDate,
        string? Remarks
    );
}
