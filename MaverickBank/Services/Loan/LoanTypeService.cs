using AutoMapper;
using MaverickBank.Data;
using MaverickBank.DTOs.Loan;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.Loan
{
    public class LoanTypeService
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

        public async Task<IEnumerable<LoanTypeResponseDto>> GetAllLoanTypesAsync()
        {
            var loanTypes = await _context.LoanTypes.ToListAsync();
            return _mapper.Map<IEnumerable<LoanTypeResponseDto>>(loanTypes);
        }

        public async Task<LoanTypeResponseDto?> GetLoanTypeByIdAsync(int loanTypeId)
        {
            var loanType = await _context.LoanTypes.FindAsync(loanTypeId);
            return loanType is null ? null : _mapper.Map<LoanTypeResponseDto>(loanType);
        }

        public async Task<LoanTypeResponseDto> CreateLoanTypeAsync(CreateLoanTypeDto dto)
        {
            var exists = await _context.LoanTypes.AnyAsync(l => l.LoanName == dto.LoanName);
            if (exists)
                throw new InvalidOperationException($"Loan type '{dto.LoanName}' already exists.");

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

            var nameConflict = await _context.LoanTypes
                .AnyAsync(l => l.LoanName == dto.LoanName && l.LoanTypeId != loanTypeId);
            if (nameConflict)
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
