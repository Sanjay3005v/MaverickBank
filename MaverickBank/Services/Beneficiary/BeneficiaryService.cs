using MaverickBank.Data;
using MaverickBank.DTOs.Beneficiary;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.Beneficiary
{
    public class BeneficiaryService : IBeneficiaryService
    {
        private readonly AppDbContext _context;

        public BeneficiaryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<BeneficiaryResponseDto> AddBeneficiaryAsync(AddBeneficiaryDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.UserId == dto.UserId);

            if (!userExists)
                throw new Exception("User not found");

            var beneficiary = new Models.Beneficiary
            {
                UserId = dto.UserId,
                BeneficiaryName = dto.BeneficiaryName,
                AccountNumber = dto.AccountNumber,
                BankName = dto.BankName,
                BranchName = dto.BranchName,
                IFSCCode = dto.IFSCCode,
                CreatedAt = DateTime.UtcNow
            };

            _context.Beneficiaries.Add(beneficiary);

            await _context.SaveChangesAsync();

            return new BeneficiaryResponseDto(
                beneficiary.BeneficiaryId,
                beneficiary.UserId,
                beneficiary.BeneficiaryName,
                beneficiary.AccountNumber,
                beneficiary.BankName,
                beneficiary.BranchName,
                beneficiary.IFSCCode,
                beneficiary.CreatedAt
            );
        }

        public async Task<IEnumerable<BeneficiaryResponseDto>>
            GetBeneficiariesByUserIdAsync(long userId)
        {
            return await _context.Beneficiaries
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BeneficiaryResponseDto(
                    b.BeneficiaryId,
                    b.UserId,
                    b.BeneficiaryName,
                    b.AccountNumber,
                    b.BankName,
                    b.BranchName,
                    b.IFSCCode,
                    b.CreatedAt
                )).ToListAsync();
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
