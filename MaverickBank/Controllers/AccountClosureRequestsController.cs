using MaverickBank.DTOs.AccountClosureRequest;
using MaverickBank.Services.AccountClosureRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MaverickBank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        public async Task<ActionResult<IEnumerable<AccountClosureRequestResponseDto>>> GetPendingRequests()
        {
            var requests = await _service.GetPendingRequestsAsync();

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
