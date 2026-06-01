using MaverickBank.Data;
using MaverickBank.DTOs.Loan;
using MaverickBank.Models;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.Loan
{
    public class LoanService : ILoanService
    {
        private readonly AppDbContext _context;

        public LoanService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LoanResponseDto> ApplyLoanAsync(ApplyLoanDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == dto.UserId);

            if (user is null)
                throw new Exception("User not found");

            var application = new Models.LoanApplication
            {
                UserId = dto.UserId,
                LoanTypeId = dto.LoanTypeId,
                RequestedAmount = dto.RequestedAmount,
                TenureMonths = dto.TenureMonths,
                Purpose = dto.Purpose,
                MonthlyIncome = dto.MonthlyIncome,
                ApplicationStatus = "Pending",
                AppliedDate = DateTime.UtcNow
            };

            _context.LoanApplications.Add(application);

            await _context.SaveChangesAsync();

            return new LoanResponseDto(
                0,
                application.LoanApplicationId,
                0,
                0,
                0,
                application.TenureMonths,
                0,
                0,
                DateTime.MinValue,
                DateTime.MinValue,
                application.ApplicationStatus
            );
        }

        public async Task<IEnumerable<LoanResponseDto>> GetLoansByUserIdAsync(int userId)
        {
            return await _context.Loans
                .Join(
                    _context.LoanApplications,
                    loan => loan.LoanApplicationId,
                    application => application.LoanApplicationId,
                    (loan, application) => new { loan, application })
                .Where(x => x.application.UserId == userId)
                .Select(x => new LoanResponseDto(
                    x.loan.LoanId,
                    x.loan.LoanApplicationId,
                    x.loan.AccountId,
                    x.loan.ApprovedAmount,
                    x.loan.InterestRate,
                    x.loan.TenureMonths,
                    x.loan.EMIAmount,
                    x.loan.OutstandingAmount,
                    x.loan.StartDate,
                    x.loan.EndDate,
                    x.loan.LoanStatus
                )).ToListAsync();
        }

        public async Task<LoanResponseDto?> GetLoanByIdAsync(int loanId)
        {
            return await _context.Loans
                .Where(l => l.LoanId == loanId)
                .Select(l => new LoanResponseDto(
                    l.LoanId,
                    l.LoanApplicationId,
                    l.AccountId,
                    l.ApprovedAmount,
                    l.InterestRate,
                    l.TenureMonths,
                    l.EMIAmount,
                    l.OutstandingAmount,
                    l.StartDate,
                    l.EndDate,
                    l.LoanStatus
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateLoanStatusAsync(int loanApplicationId,ApproveLoanDto dto)
        {
            var application = await _context.LoanApplications.FirstOrDefaultAsync(l =>l.LoanApplicationId == loanApplicationId);

            if (application is null)
                return false;

            application.ApplicationStatus = "Approved";

            var account = await _context.Accounts.FirstOrDefaultAsync(a =>a.UserId == application.UserId);

            if (account is null)
                throw new Exception("User account not found");

            var loan = new Models.Loan
            {
                LoanApplicationId = application.LoanApplicationId,
                AccountId = account.AccountId,
                ApprovedAmount = dto.ApprovedAmount,
                InterestRate = dto.InterestRate,
                TenureMonths = dto.TenureMonths,
                EMIAmount = dto.ApprovedAmount / dto.TenureMonths,
                OutstandingAmount = dto.ApprovedAmount,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(dto.TenureMonths),
                LoanStatus = "Active"
            };

            account.Balance += dto.ApprovedAmount;

            _context.Loans.Add(loan);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
