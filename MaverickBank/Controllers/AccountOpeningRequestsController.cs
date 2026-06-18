using Asp.Versioning;
using MaverickBank.DTOs.AccountOpeningRequestDto;
using MaverickBank.DTOs.Pagination;
using MaverickBank.Extensions;
using MaverickBank.Services.AccountOpeningRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MaverickBank.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class AccountOpeningRequestsController : ControllerBase
    {
        private readonly IAccountOpeningRequestService _service;

        public AccountOpeningRequestsController(IAccountOpeningRequestService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<AccountOpeningRequestResponseDto>> CreateRequest(CreateAccountOpeningRequestDto dto)
        {
            if (User.GetUserId() != dto.UserId)
                return BadRequest(new { message = "You can only request an account on your own behalf." });

            var request = await _service.CreateRequestAsync(dto);
            return Ok(request);
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<ActionResult<PagedResultDto<AccountOpeningRequestResponseDto>>> GetPendingRequests(
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var requests = await _service.GetPendingRequestsAsync(pageNumber, pageSize);
            return Ok(requests);
        }

        [HttpGet("user/{userId:int}")]
        [Authorize]
        public async Task<ActionResult<PagedResultDto<AccountOpeningRequestResponseDto>>> GetRequestsByUserId(
            int userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (!User.CanAccessUser(userId))
                return Forbid();

            var requests = await _service.GetRequestsByUserIdAsync(userId, pageNumber, pageSize);
            return Ok(requests);
        }

        [HttpPut("{requestId:long}/approve")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> ApproveRequest(long requestId, ReviewAccountOpeningRequestDto dto)
        {
            var approved = await _service.ApproveRequestAsync(requestId, User.GetUserId(), dto.Remarks);

            if (!approved)
                return NotFound(new { message = $"Account opening request with ID {requestId} not found." });

            return NoContent();
        }

        [HttpPut("{requestId:long}/reject")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> RejectRequest(long requestId, ReviewAccountOpeningRequestDto dto)
        {
            var rejected = await _service.RejectRequestAsync(requestId, User.GetUserId(), dto.Remarks);

            if (!rejected)
                return NotFound(new { message = $"Account opening request with ID {requestId} not found." });

            return NoContent();
        }
    }
}
