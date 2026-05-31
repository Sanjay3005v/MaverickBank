namespace MaverickBank.DTOs.Branch
{
    public record CreateBranchDto(
        string BranchName,
        string IFSCCode,
        string AddressLine1,
        string City,
        string State,
        string Pincode,
        string PhoneNumber
    );
}
