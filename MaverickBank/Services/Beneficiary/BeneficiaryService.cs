using AutoMapper;
using MaverickBank.Data;
using MaverickBank.DTOs.Beneficiary;
using MaverickBank.DTOs.Pagination;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.Beneficiary
{
    public class BeneficiaryService : IBeneficiaryService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<BeneficiaryService> _logger;

        public BeneficiaryService(AppDbContext context, IMapper mapper, ILogger<BeneficiaryService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BeneficiaryResponseDto> AddBeneficiaryAsync(AddBeneficiaryDto dto)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == dto.UserId))
                throw new KeyNotFoundException($"User with ID {dto.UserId} not found.");

            var userExists = await _context.Users.AnyAsync(u => u.UserId == dto.UserId);

            if (!userExists)
                throw new Exception("User not found");

            var beneficiary = _mapper.Map<Models.Beneficiary>(dto);
            beneficiary.CreatedAt = DateTime.UtcNow;

            _context.Beneficiaries.Add(beneficiary);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Added beneficiary {BeneficiaryId} for user {UserId}", beneficiary.BeneficiaryId, dto.UserId);
            return _mapper.Map<BeneficiaryResponseDto>(beneficiary);
        }

        public async Task<PagedResultDto<BeneficiaryResponseDto>> GetBeneficiariesByUserIdAsync(long userId, int pageNumber, int pageSize)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var totalCount = await _context.Beneficiaries.CountAsync();
            var items = await _context.Beneficiaries
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = _mapper.Map<IEnumerable<BeneficiaryResponseDto>>(items);
            return new PagedResultDto<BeneficiaryResponseDto>(
                data, pageNumber, pageSize, totalCount,
                (int)Math.Ceiling(totalCount / (double)pageSize));
        }
        public async Task<bool> DeleteBeneficiaryAsync(long beneficiaryId)
        {
            var beneficiary = await _context.Beneficiaries
                .FirstOrDefaultAsync(b => b.BeneficiaryId == beneficiaryId);

            if (beneficiary is null)
                return false;

            _context.Beneficiaries.Remove(beneficiary);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted beneficiary {BeneficiaryId}", beneficiaryId);

            return true;
        }
    }
}
