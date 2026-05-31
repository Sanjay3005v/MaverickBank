namespace MaverickBank.DTOs.Loan
{
    public record ApplyLoanDto(
        int UserId,
        int LoanTypeId,
        decimal RequestedAmount,
        int TenureMonths,
        string Purpose,
        decimal MonthlyIncome
    );
}
