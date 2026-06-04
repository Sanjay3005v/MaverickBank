using MaverickBank.DTOs.User;

namespace MaverickBank.Services.User
{
    public interface IUserService
    {
        Task<UserResponseDto> RegisterAsync(CreateUserDto dto);
        Task<UserResponseDto?> GetUserByIdAsync(int userId);
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<bool> UpdateUserAsync(int userId, UpdateUserDto dto);
        Task<bool> SetUserActiveStatusAsync(int userId, bool isActive);
    }
}
