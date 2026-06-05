using MaverickBank.DTOs.Branch;
using MaverickBank.DTOs.Pagination;

namespace MaverickBank.Services.Branch
{
    public interface IBranchService
    {
        Task<PagedResultDto<BranchResponseDto>> GetAllBranchesAsync(int pageNumber, int pageSize);
        Task<BranchResponseDto?> GetBranchByIdAsync(int branchId);
        Task<BranchResponseDto> CreateBranchAsync(CreateBranchDto dto);
        Task<bool> UpdateBranchAsync(int branchId, CreateBranchDto dto);
        Task<bool> DeleteBranchAsync(int branchId);
    }
}
