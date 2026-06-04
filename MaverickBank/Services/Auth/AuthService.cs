using MaverickBank.Data;
using MaverickBank.DTOs.Auth;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AppDbContext context,
            IJwtTokenService jwtTokenService,
            IConfiguration config,
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _config = config;
            _logger = logger;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user is null || !user.IsActive)
                throw new UnauthorizedAccessException("Invalid email or password.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            var role = await _context.Roles.FindAsync(user.RoleId);
            if (role is null)
                throw new UnauthorizedAccessException("User role not found.");

            var expiryMinutes = int.Parse(_config["JwtSettings:ExpiryMinutes"]!);
            var token = _jwtTokenService.GenerateToken(user, role.RoleName);

            _logger.LogInformation("User {UserId} logged in successfully", user.UserId);

            return new LoginResponseDto(
                Token: token,
                Email: user.Email,
                Role: role.RoleName,
                UserId: user.UserId,
                ExpiresAt: DateTime.UtcNow.AddMinutes(expiryMinutes)
            );
        }
    }
}
