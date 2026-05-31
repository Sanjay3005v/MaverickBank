namespace MaverickBank.DTOs.Loan
{
    public record LoanRepaymentDto(
        long LoanId,
        decimal AmountPaid,
        string PaymentMethod,
        string? Remarks
    );
}
