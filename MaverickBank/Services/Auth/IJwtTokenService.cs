using MaverickBank.Models;

namespace MaverickBank.Services.Auth
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user, string roleName);
    }
}
