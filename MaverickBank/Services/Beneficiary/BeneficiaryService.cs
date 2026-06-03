using AutoMapper;
using MaverickBank.Data;
using MaverickBank.DTOs.Beneficiary;
using MaverickBank.Services.Account;
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

        public async Task<IEnumerable<BeneficiaryResponseDto>> GetBeneficiariesByUserIdAsync(long userId)
        {
            var beneficiaries = await _context.Beneficiaries
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<BeneficiaryResponseDto>>(beneficiaries);
        }

        public async Task<bool> DeleteBeneficiaryAsync(long beneficiaryId)
        {
            var beneficiary = await _context.Beneficiaries
                .FirstOrDefaultAsync(b => b.BeneficiaryId == beneficiaryId);

            if (beneficiary is null)
                return false;

            _context.Beneficiaries.Remove(beneficiary);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
