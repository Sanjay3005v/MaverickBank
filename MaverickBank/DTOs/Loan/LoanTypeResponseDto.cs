namespace MaverickBank.DTOs.Loan
{
    public record LoanTypeResponseDto(
        int LoanTypeId,
        string LoanName,
        decimal InterestRate,
        decimal MinimumAmount,
        decimal MaximumAmount,
        int MinimumTenureMonths,
        int MaximumTenureMonths
    );
}
