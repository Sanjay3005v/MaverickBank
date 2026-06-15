namespace MaverickBank.DTOs.Auth
{
    public record ResetPasswordDto(string Email, string Otp, string NewPassword);
}