using MaverickBank.DTOs.Pagination;
using MaverickBank.DTOs.AuditLog;

namespace MaverickBank.Services.AuditLog
{
    public interface IAuditLogService
    {
        Task LogAsync(int userId, string action, string entityName, long entityId, string? oldValues = null, string? newValues = null);
        Task<PagedResultDto<AuditLogResponseDto>> GetAllAsync(int pageNumber, int pageSize);
        Task<PagedResultDto<AuditLogResponseDto>> GetByUserIdAsync(int userId, int pageNumber, int pageSize);
    }
}
