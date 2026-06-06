using MaverickBank.DTOs.Pagination;
using MaverickBank.DTOs.AuditLog; 
using MaverickBank.Services.AuditLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaverickBank.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagedResultDto<AuditLogResponseDto>>> GetAll(
    [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _auditLogService.GetAllAsync(pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<PagedResultDto<AuditLogResponseDto>>> GetByUser(
            int userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _auditLogService.GetByUserIdAsync(userId, pageNumber, pageSize);
            return Ok(result);
        }
    }
}
