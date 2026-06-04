namespace MaverickBank.DTOs.Loan
{
    public record CreateLoanTypeDto(
        string LoanName,
        decimal InterestRate,
        decimal MinimumAmount,
        decimal MaximumAmount,
        int MinimumTenureMonths,
        int MaximumTenureMonths
    );
}
