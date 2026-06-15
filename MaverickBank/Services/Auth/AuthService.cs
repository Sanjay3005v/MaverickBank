using MaverickBank.Data;
using MaverickBank.DTOs.Auth;
using MaverickBank.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IJwtTokenService jwtTokenService, IConfiguration config, ILogger<AuthService> logger, IEmailService emailService)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _config = config;
            _logger = logger;
            _emailService = emailService;

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
        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user is null)
            {
                _logger.LogInformation("Password reset requested for unknown email {Email}", dto.Email);
                return;
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();

            _context.PasswordResetOtps.Add(new Models.PasswordResetOtp
            {
                UserId = user.UserId,
                Otp = otp,
                ExpiryDate = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            var body = $"Your MaverickBank password reset OTP is: {otp}\nThis code expires in 10 minutes.\nIf you did not request this, please ignore this email.";
            await _emailService.SendEmailAsync(user.Email, "MaverickBank Password Reset OTP", body);

            _logger.LogInformation("Password reset OTP sent to user {UserId}", user.UserId);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user is null)
                throw new InvalidOperationException("Invalid or expired OTP.");

            var resetEntry = await _context.PasswordResetOtps
                .Where(o => o.UserId == user.UserId && o.Otp == dto.Otp && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (resetEntry is null || resetEntry.ExpiryDate < DateTime.UtcNow)
                throw new InvalidOperationException("Invalid or expired OTP.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            resetEntry.IsUsed = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset completed for user {UserId}", user.UserId);
        }
    }
}
