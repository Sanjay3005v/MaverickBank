namespace MaverickBank.DTOs.User
{
    public record CreateUserDto(
        string FirstName,
        string? LastName,
        string Email,
        string PhoneNumber,
        string Password,
        string Gender,
        DateTime DateOfBirth,
        string AadhaarNumber,
        string PANNumber,
        string AddressLine1,
        string? AddressLine2,
        string City,
        string State,
        string Pincode
    );
}
