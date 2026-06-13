using System.Security.Claims;

namespace MaverickBank.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User identifier not found in token.");

            return int.Parse(value);
        }

        public static bool IsStaff(this ClaimsPrincipal user)
        {
            return user.IsInRole("Admin") || user.IsInRole("Employee");
        }


        public static bool CanAccessUser(this ClaimsPrincipal user, int targetUserId)
        {
            return user.IsStaff() || user.GetUserId() == targetUserId;
        }
    }
}
