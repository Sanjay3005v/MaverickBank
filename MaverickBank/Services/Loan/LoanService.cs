using AutoMapper;
using MaverickBank.Data;
using MaverickBank.DTOs.Loan;
using MaverickBank.DTOs.Transaction;
using MaverickBank.Models;
using Microsoft.EntityFrameworkCore;

namespace MaverickBank.Services.Loan
{
    public class LoanService : ILoanService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<LoanService> _logger;

        public LoanService(AppDbContext context, IMapper mapper, ILogger<LoanService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<LoanResponseDto> ApplyLoanAsync(ApplyLoanDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId)
                ?? throw new KeyNotFoundException("User not found.");

            var application = _mapper.Map<Models.LoanApplication>(dto);
            application.ApplicationStatus = "Pending";
            application.AppliedDate = DateTime.UtcNow;

            _context.LoanApplications.Add(application);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Loan application {AppId} submitted by user {UserId}", application.LoanApplicationId, dto.UserId);
            return _mapper.Map<LoanResponseDto>(application);
        }

        public async Task<IEnumerable<LoanResponseDto>> GetLoansByUserIdAsync(int userId)
        {
            var loans = await _context.Loans
                .Join(
                    _context.LoanApplications,
                    loan => loan.LoanApplicationId,
                    app => app.LoanApplicationId,
                    (loan, app) => new { loan, app })
                .Where(x => x.app.UserId == userId)
                .Select(x => x.loan)
                .ToListAsync();

            return _mapper.Map<IEnumerable<LoanResponseDto>>(loans);
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
                )).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateLoanStatusAsync(int loanApplicationId, ApproveLoanDto dto)
        {
            var application = await _context.LoanApplications.FirstOrDefaultAsync(l => l.LoanApplicationId == loanApplicationId);

            if (application is null)
                return false;

            application.ApplicationStatus = "Approved";
            application.ReviewedBy = dto.ReviewedBy;
            application.ReviewedDate = DateTime.UtcNow;
            application.Remarks = dto.Remarks;

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == application.UserId)
                ?? throw new KeyNotFoundException("User account not found.");

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

            _logger.LogInformation("Loan approved for application {AppId}", loanApplicationId);
            return true;
        }

        public async Task<bool> RepayLoanAsync(LoanRepaymentDto dto)
        {
            var loan = await _context.Loans.FindAsync(dto.LoanId);

            if (loan is null)
                return false;

            if (dto.AmountPaid <= 0)
                throw new Exception("Amount must be greater than zero");

            if (dto.AmountPaid > loan.OutstandingAmount)
                throw new Exception("Amount exceeds outstanding balance");

            loan.OutstandingAmount -= dto.AmountPaid;

            if (loan.OutstandingAmount == 0)
            {
                loan.LoanStatus = "Closed";
            }

            var repayment = new Models.LoanRepayment
            {
                LoanId = dto.LoanId,
                AmountPaid = dto.AmountPaid,
                PaymentMethod = dto.PaymentMethod,
                Remarks = dto.Remarks,
                PaymentDate = DateTime.UtcNow
            };

            _context.LoanRepayments.Add(repayment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Repayment of {Amount} made for loan {LoanId}", dto.AmountPaid, dto.LoanId);
            return true;
        }

        public async Task<bool> RejectLoanAsync(int loanApplicationId, RejectLoanDto dto)
        {
            var application = await _context.LoanApplications
                .FirstOrDefaultAsync(l => l.LoanApplicationId == loanApplicationId);

            if (application is null)
                return false;

            if (application.ApplicationStatus != "Pending")
                throw new InvalidOperationException("Only pending applications can be rejected.");

            application.ApplicationStatus = "Rejected";
            application.ReviewedBy = dto.ReviewedBy;
            application.ReviewedDate = DateTime.UtcNow;
            application.Remarks = dto.Remarks;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Loan application {AppId} rejected by {ReviewedBy}", loanApplicationId, dto.ReviewedBy);
            return true;
        }

        public async Task<IEnumerable<LoanResponseDto>> GetPendingLoanApplicationsAsync()
        {
            var applications = await _context.LoanApplications
                .Where(l => l.ApplicationStatus == "Pending")
                .OrderBy(l => l.AppliedDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<LoanResponseDto>>(applications);
        }
    }
}
