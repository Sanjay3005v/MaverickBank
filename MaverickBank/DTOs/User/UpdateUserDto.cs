namespace MaverickBank.DTOs.User
{
    public record UpdateUserDto(
        string FirstName,
        string? LastName,
        string PhoneNumber,
        string AddressLine1,
        string? AddressLine2,
        string City,
        string State,
        string Pincode
    );
}
