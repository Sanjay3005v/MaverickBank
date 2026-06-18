namespace MaverickBank.Services.Report
{
    public interface IReportService
    {
        Task<byte[]> GenerateTransactionReportAsync(long accountId, DateTime? from, DateTime? to);

    }
}
