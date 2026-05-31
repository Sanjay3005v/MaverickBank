namespace MaverickBank.DTOs.Loan
{
    public record LoanResponseDto(
        long LoanId,
        long LoanApplicationId,
        long AccountId,
        decimal ApprovedAmount,
        decimal InterestRate,
        int TenureMonths,
        decimal EMIAmount,
        decimal OutstandingAmount,
        DateTime StartDate,
        DateTime EndDate,
        string LoanStatus
    );
}
