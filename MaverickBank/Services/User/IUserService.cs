using MaverickBank.DTOs.Pagination;
using MaverickBank.DTOs.User;

namespace MaverickBank.Services.User
{
    public interface IUserService
    {
        Task<UserResponseDto> RegisterAsync(CreateUserDto dto);
        Task<UserResponseDto?> GetUserByIdAsync(int userId);
        Task<PagedResultDto<UserResponseDto>> GetAllUsersAsync(int pageNumber, int pageSize);
        Task<bool> UpdateUserAsync(int userId, UpdateUserDto dto);
        Task<bool> SetUserActiveStatusAsync(int userId, bool isActive);
        Task<UserResponseDto> RegisterEmployeeAsync(CreateUserDto dto, int createdByAdminId);

    }
}
