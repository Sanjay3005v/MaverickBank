using Asp.Versioning;
using MaverickBank.DTOs.AccountClosureRequest;
using MaverickBank.DTOs.Pagination;
using MaverickBank.Services.AccountClosureRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MaverickBank.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class AccountClosureRequestsController : ControllerBase
    {
        private readonly IAccountClosureRequestService _service;

        public AccountClosureRequestsController(IAccountClosureRequestService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<ActionResult<AccountClosureRequestResponseDto>> CreateRequest(CreateAccountClosureRequestDto dto)
        {
            var request = await _service.CreateRequestAsync(dto);

            return Ok(request);
        }

        [HttpGet("pending")]
        public async Task<ActionResult<PagedResultDto<AccountClosureRequestResponseDto>>> GetPendingRequests([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var requests = await _service.GetPendingRequestsAsync(pageNumber, pageSize);
            return Ok(requests);
        }

        [HttpPut("{requestId:long}/approve")]
        public async Task<IActionResult> ApproveRequest(long requestId, [FromQuery] int reviewedBy, [FromQuery] string remarks)
        {
            var approved = await _service.ApproveRequestAsync(requestId, reviewedBy, remarks);

            if (!approved)
                return NotFound();

            return NoContent();
        }
    }
}
