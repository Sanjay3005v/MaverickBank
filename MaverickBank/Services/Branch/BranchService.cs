using AutoMapper;
using MaverickBank.Data;
using MaverickBank.DTOs.Branch;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.Branch
{
    public class BranchService : IBranchService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<BranchService> _logger;

        public BranchService(AppDbContext context, IMapper mapper, ILogger<BranchService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<BranchResponseDto>> GetAllBranchesAsync()
        {
            var branches = await _context.Branches.ToListAsync();
            return _mapper.Map<IEnumerable<BranchResponseDto>>(branches);
        }

        public async Task<BranchResponseDto?> GetBranchByIdAsync(int branchId)
        {
            var branch = await _context.Branches.FindAsync(branchId);
            return branch is null ? null : _mapper.Map<BranchResponseDto>(branch);
        }

        public async Task<BranchResponseDto> CreateBranchAsync(CreateBranchDto dto)
        {
            if (await _context.Branches.AnyAsync(b => b.IFSCCode == dto.IFSCCode))
                throw new InvalidOperationException($"A branch with IFSC code '{dto.IFSCCode}' already exists.");

            var branch = _mapper.Map<Models.Branch>(dto);
            branch.CreatedAt = DateTime.UtcNow;

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created branch with ID {BranchId}", branch.BranchId);
            return _mapper.Map<BranchResponseDto>(branch);
        }

        public async Task<bool> UpdateBranchAsync(int branchId, CreateBranchDto dto)
        {
            var branch = await _context.Branches.FindAsync(branchId);
            if (branch is null)
                return false;

            if (await _context.Branches.AnyAsync(b => b.IFSCCode == dto.IFSCCode && b.BranchId != branchId))
                throw new InvalidOperationException($"Another branch already uses IFSC code '{dto.IFSCCode}'.");

            _mapper.Map(dto, branch);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated branch with ID {BranchId}", branchId);
            return true;
        }

        public async Task<bool> DeleteBranchAsync(int branchId)
        {
            var branch = await _context.Branches.FindAsync(branchId);
            if (branch is null)
                return false;

            _context.Branches.Remove(branch);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted branch with ID {BranchId}", branchId);
            return true;
        }
    }
}
