namespace MaverickBank.DTOs.Branch
{
    public record BranchResponseDto(
        int BranchId,
        string BranchName,
        string IFSCCode,
        string AddressLine1,
        string City,
        string State,
        string Pincode,
        string PhoneNumber
    );
}
