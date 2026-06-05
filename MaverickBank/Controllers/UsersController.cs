using Asp.Versioning;
using MaverickBank.DTOs.User;
using MaverickBank.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MaverickBank.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserResponseDto>> Register(CreateUserDto dto)
        {
            var user = await _userService.RegisterAsync(dto);
            return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, user);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<UserResponseDto>> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user is null)
                return NotFound(new { message = $"User with ID {id} not found." });

            return Ok(user);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers([FromQuery] int pageNumber = 1,[FromQuery] int pageSize = 10)
        {
            var users = await _userService.GetAllUsersAsync(pageNumber, pageSize);
            return Ok(users);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto dto)
        {
            var updated = await _userService.UpdateUserAsync(id, dto);
            if (!updated)
                return NotFound(new { message = $"User with ID {id} not found." });

            return NoContent();
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetActiveStatus(int id, [FromQuery] bool isActive)
        {
            var updated = await _userService.SetUserActiveStatusAsync(id, isActive);
            if (!updated)
                return NotFound(new { message = $"User with ID {id} not found." });

            return NoContent();
        }
    }
}
