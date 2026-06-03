using MaverickBank.DTOs.Branch;

namespace MaverickBank.Services.Branch
{
    public interface IBranchService
    {
        Task<IEnumerable<BranchResponseDto>> GetAllBranchesAsync();
        Task<BranchResponseDto?> GetBranchByIdAsync(int branchId);
        Task<BranchResponseDto> CreateBranchAsync(CreateBranchDto dto);
        Task<bool> UpdateBranchAsync(int branchId, CreateBranchDto dto);
        Task<bool> DeleteBranchAsync(int branchId);
    }
}
