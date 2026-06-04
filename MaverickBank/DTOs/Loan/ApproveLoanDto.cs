namespace MaverickBank.DTOs.Loan
{
    public record ApproveLoanDto(
        decimal ApprovedAmount,
        decimal InterestRate,
        int TenureMonths,
        string Remarks,
        int ReviewedBy
    );
}
