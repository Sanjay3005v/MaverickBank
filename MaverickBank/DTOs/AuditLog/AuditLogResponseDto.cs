namespace MaverickBank.DTOs.AuditLog
{
    public record AuditLogResponseDto(
        long AuditLogId,
        int UserId,
        string Action,
        string EntityName,
        long EntityId,
        string? OldValues,
        string? NewValues,
        DateTime ActionDate
    );
}
