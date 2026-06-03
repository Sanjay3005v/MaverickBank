namespace MaverickBank.DTOs.Auth
{
    public record LoginResponseDto(
        string Token,
        string Email,
        string Role,
        int UserId,
        DateTime ExpiresAt
    );

}
