using AutoMapper;
using MaverickBank.Data;
using MaverickBank.DTOs.Pagination;
using MaverickBank.DTOs.AuditLog;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.AuditLog
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(AppDbContext context, IMapper mapper, ILogger<AuditLogService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task LogAsync(int userId, string action, string entityName,
                                   long entityId, string? oldValues = null, string? newValues = null)
        {
            try
            {
                _context.AuditLogs.Add(new Models.AuditLog
                {
                    UserId = userId,
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    OldValues = oldValues,
                    NewValues = newValues,
                    ActionDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log for action {Action}", action);
            }
        }
        public async Task<PagedResultDto<AuditLogResponseDto>> GetAllAsync(int pageNumber, int pageSize)
        {
            var totalCount = await _context.AuditLogs.CountAsync();

            var items = await _context.AuditLogs
                .OrderByDescending(a => a.ActionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = _mapper.Map<IEnumerable<AuditLogResponseDto>>(items);
            return new PagedResultDto<AuditLogResponseDto>(
                data, pageNumber, pageSize, totalCount,
                (int)Math.Ceiling(totalCount / (double)pageSize));
        }

        public async Task<PagedResultDto<AuditLogResponseDto>> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            var totalCount = await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .CountAsync();

            var items = await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.ActionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = _mapper.Map<IEnumerable<AuditLogResponseDto>>(items);
            return new PagedResultDto<AuditLogResponseDto>(
                data, pageNumber, pageSize, totalCount,
                (int)Math.Ceiling(totalCount / (double)pageSize));
        }
    }
}
