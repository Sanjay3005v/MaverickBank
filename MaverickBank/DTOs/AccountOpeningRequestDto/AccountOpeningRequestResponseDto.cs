namespace MaverickBank.DTOs.AccountOpeningRequestDto
{
    public record AccountOpeningRequestResponseDto(
        long RequestId,
        int UserId,
        int BranchId,
        int AccountTypeId,
        decimal InitialDeposit,
        DateTime RequestDate,
        string Status,
        int? ReviewedBy,
        DateTime? ReviewedDate,
        string? Remarks,
        long? CreatedAccountId
    );
}
