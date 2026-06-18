namespace MaverickBank.DTOs.Report
{
    public record TransactionReportRequestDto(
        DateTime? From,
        DateTime? To
    );
}
