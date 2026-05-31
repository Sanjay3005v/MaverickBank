namespace MaverickBank.DTOs.User
{
    public record UserResponseDto(
        int UserId,
        int RoleId,
        string FirstName,
        string? LastName,
        string Email,
        string PhoneNumber,
        string Gender,
        DateTime DateOfBirth,
        string AadhaarNumber,
        string PANNumber,
        string AddressLine1,
        string? AddressLine2,
        string City,
        string State,
        string Pincode,
        bool IsActive
    );
}
