using MaverickBank.Models;

namespace MaverickBank.Services.Auth
{
    public interface IJwtTokenService
    {
        string GenerateToken(Models.User user, string roleName);
    }
}
