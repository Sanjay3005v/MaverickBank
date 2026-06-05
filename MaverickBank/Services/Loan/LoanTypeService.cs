using AutoMapper;
using MaverickBank.Data;
using MaverickBank.DTOs.Loan;
using MaverickBank.DTOs.Pagination;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.Loan
{
    public class LoanTypeService : ILoanTypeService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<LoanTypeService> _logger;

        public LoanTypeService(AppDbContext context, IMapper mapper, ILogger<LoanTypeService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResultDto<LoanTypeResponseDto>> GetAllLoanTypesAsync(int pageNumber, int pageSize)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var totalCount = await _context.LoanTypes.CountAsync();
            var items = await _context.LoanTypes
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = _mapper.Map<IEnumerable<LoanTypeResponseDto>>(items);
            return new PagedResultDto<LoanTypeResponseDto>(
                data, pageNumber, pageSize, totalCount,
                (int)Math.Ceiling(totalCount / (double)pageSize));
        }
        public async Task<LoanTypeResponseDto?> GetLoanTypeByIdAsync(int loanTypeId)
        {
            var loanType = await _context.LoanTypes.FindAsync(loanTypeId);
            return loanType is null ? null : _mapper.Map<LoanTypeResponseDto>(loanType);
        }

        public async Task<LoanTypeResponseDto> CreateLoanTypeAsync(CreateLoanTypeDto dto)
        {
            if (await _context.LoanTypes.AnyAsync(l => l.LoanName == dto.LoanName))
                throw new InvalidOperationException($"Loan type '{dto.LoanName}' already exists.");

            if (dto.MinimumAmount >= dto.MaximumAmount)
                throw new InvalidOperationException("Minimum amount must be less than maximum amount.");

            if (dto.MinimumTenureMonths >= dto.MaximumTenureMonths)
                throw new InvalidOperationException("Minimum tenure must be less than maximum tenure.");

            var loanType = _mapper.Map<Models.LoanType>(dto);

            _context.LoanTypes.Add(loanType);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created loan type {LoanTypeId}", loanType.LoanTypeId);
            return _mapper.Map<LoanTypeResponseDto>(loanType);
        }

        public async Task<bool> UpdateLoanTypeAsync(int loanTypeId, CreateLoanTypeDto dto)
        {
            var loanType = await _context.LoanTypes.FindAsync(loanTypeId);
            if (loanType is null)
                return false;

            if (await _context.LoanTypes.AnyAsync(l => l.LoanName == dto.LoanName && l.LoanTypeId != loanTypeId))
                throw new InvalidOperationException($"Another loan type already uses the name '{dto.LoanName}'.");

            _mapper.Map(dto, loanType);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated loan type {LoanTypeId}", loanTypeId);
            return true;
        }

        public async Task<bool> DeleteLoanTypeAsync(int loanTypeId)
        {
            var loanType = await _context.LoanTypes.FindAsync(loanTypeId);
            if (loanType is null)
                return false;

            _context.LoanTypes.Remove(loanType);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted loan type {LoanTypeId}", loanTypeId);
            return true;
        }
    }
}
