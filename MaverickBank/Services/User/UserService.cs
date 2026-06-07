using AutoMapper;
using MaverickBank.Data;
using MaverickBank.DTOs.Pagination;
using MaverickBank.DTOs.User;
using MaverickBank.Services.AuditLog;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace MaverickBank.Services.User
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<UserService> _logger;

        public UserService(AppDbContext context, IMapper mapper, IAuditLogService auditLogService, ILogger<UserService> logger)
        {
            _context = context;
            _mapper = mapper;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<UserResponseDto> RegisterAsync(CreateUserDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException("Email is already registered.");

            if (await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
                throw new InvalidOperationException("Phone number is already registered.");

            if (await _context.Users.AnyAsync(u => u.AadhaarNumber == dto.AadhaarNumber))
                throw new InvalidOperationException("Aadhaar number is already registered.");

            if (await _context.Users.AnyAsync(u => u.PANNumber == dto.PANNumber))
                throw new InvalidOperationException("PAN number is already registered.");

            if (!await _context.Roles.AnyAsync(r => r.RoleId == dto.RoleId))
                throw new KeyNotFoundException("Role not found.");

            var user = _mapper.Map<Models.User>(dto);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            user.IsActive = false;
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(user.UserId, "User Registered", "User", user.UserId, newValues: JsonSerializer.Serialize(dto));

            _logger.LogInformation("User {UserId} registered with role {RoleId}", user.UserId, user.RoleId);
            return _mapper.Map<UserResponseDto>(user);
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user is null ? null : _mapper.Map<UserResponseDto>(user);
        }

        public async Task<PagedResultDto<UserResponseDto>> GetAllUsersAsync(int pageNumber, int pageSize)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var totalCount = await _context.Users.CountAsync();
            var users = await _context.Users
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = _mapper.Map<IEnumerable<UserResponseDto>>(users);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResultDto<UserResponseDto>(data, pageNumber, pageSize, totalCount, totalPages);
        }

        public async Task<bool> UpdateUserAsync(int userId, UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user is null)
                return false;

            if (await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber && u.UserId != userId))
                throw new InvalidOperationException("Phone number is already in use.");

            var oldValues = JsonSerializer.Serialize(_mapper.Map<UpdateUserDto>(user));

            _mapper.Map(dto, user);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(userId, "User Updated", "User", userId,
                oldValues: oldValues,
                newValues: JsonSerializer.Serialize(dto));
            _logger.LogInformation("User {UserId} updated", userId);
            return true;
        }

        public async Task<bool> SetUserActiveStatusAsync(int userId, bool isActive)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user is null)
                return false;

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(userId, $"User {(isActive ? "Activated" : "Deactivated")}", "User", userId);
            _logger.LogInformation("User {UserId} active status set to {IsActive}", userId, isActive);
            return true;
        }
    }
}
