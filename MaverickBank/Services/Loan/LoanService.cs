using AutoMapper;
using MaverickBank.Data;
using MaverickBank.DTOs.Loan;
using MaverickBank.DTOs.Pagination;
using MaverickBank.DTOs.Transaction;
using MaverickBank.Models;
using MaverickBank.Services.AuditLog;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MaverickBank.Services.Loan
{
    public class LoanService : ILoanService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<LoanService> _logger;

        public LoanService(AppDbContext context, IMapper mapper, IAuditLogService auditLogService, ILogger<LoanService> logger)
        {
            _context = context;
            _mapper = mapper;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<LoanResponseDto> ApplyLoanAsync(ApplyLoanDto dto)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == dto.UserId))
                throw new KeyNotFoundException("User not found.");

            var loanType = await _context.LoanTypes.FindAsync(dto.LoanTypeId)
                ?? throw new KeyNotFoundException("Loan type not found.");

            if (dto.RequestedAmount < loanType.MinimumAmount || dto.RequestedAmount > loanType.MaximumAmount)
                throw new InvalidOperationException(
                    $"Requested amount must be between {loanType.MinimumAmount} and {loanType.MaximumAmount}.");

            if (dto.TenureMonths < loanType.MinimunTenureMonths || dto.TenureMonths > loanType.MaximumTenureMonths)
                throw new InvalidOperationException(
                    $"Tenure must be between {loanType.MinimunTenureMonths} and {loanType.MaximumTenureMonths} months.");

            var application = _mapper.Map<Models.LoanApplication>(dto);
            application.ApplicationStatus = "Pending";
            application.AppliedDate = DateTime.UtcNow;

            _context.LoanApplications.Add(application);

            await _context.SaveChangesAsync();

            var resultDto = _mapper.Map<LoanResponseDto>(application);

            await _auditLogService.LogAsync(dto.UserId, "Loan Application Submitted", "LoanApplication", application.LoanApplicationId, newValues: JsonSerializer.Serialize(resultDto));

            _logger.LogInformation("Loan application {AppId} submitted by user {UserId}", application.LoanApplicationId, dto.UserId);
            return _mapper.Map<LoanResponseDto>(application);
        }

        public async Task<PagedResultDto<LoanResponseDto>> GetLoansByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var totalCount = await _context.Loans
                .Join(
                    _context.LoanApplications,
                    loan => loan.LoanApplicationId,
                    app => app.LoanApplicationId,
                    (loan, app) => new { loan, app })
                .Where(x => x.app.UserId == userId)
                .Select(x => x.loan).CountAsync();
            var items = await _context.Loans
                .Join(
                    _context.LoanApplications,
                    loan => loan.LoanApplicationId,
                    app => app.LoanApplicationId,
                    (loan, app) => new { loan, app })
                .Where(x => x.app.UserId == userId)
                .Select(x => x.loan)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = _mapper.Map<IEnumerable<LoanResponseDto>>(items);
            return new PagedResultDto<LoanResponseDto>(
                data, pageNumber, pageSize, totalCount,
                (int)Math.Ceiling(totalCount / (double)pageSize));
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

            if (application.ApplicationStatus != "Pending")
                throw new InvalidOperationException("Only pending applications can be approved.");

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == application.UserId) ?? throw new KeyNotFoundException("User account not found.");

            application.ApplicationStatus = "Approved";
            application.ReviewedBy = dto.ReviewedBy;
            application.ReviewedDate = DateTime.UtcNow;
            application.Remarks = dto.Remarks;



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
            await _auditLogService.LogAsync(dto.ReviewedBy, "Loan Approved", "Loan", loan.LoanId);

            _logger.LogInformation("Loan approved for application {AppId}", loanApplicationId);
            return true;
        }

        public async Task<bool> RepayLoanAsync(LoanRepaymentDto dto)
        {
            var loan = await _context.Loans.FindAsync(dto.LoanId);

            if (loan is null)
                return false;

            if (dto.AmountPaid <= 0)
                throw new Exception("Amount must be greater than zero.");

            if (dto.AmountPaid > loan.OutstandingAmount)
                throw new Exception("Amount exceeds outstanding balance.");

            loan.OutstandingAmount -= dto.AmountPaid;

            if (loan.OutstandingAmount == 0)
                loan.LoanStatus = "Closed";


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

            var account = await _context.Accounts.FindAsync(loan.AccountId);

            await _auditLogService.LogAsync(account!.UserId, "Loan Repayment Made", "Loan", dto.LoanId, newValues: $"Amount: {dto.AmountPaid}"); _logger.LogInformation("Repayment of {Amount} made for loan {LoanId}", dto.AmountPaid, dto.LoanId);
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

            await _auditLogService.LogAsync(dto.ReviewedBy, "Loan Rejected", "Loan", loanApplicationId);

            _logger.LogInformation("Loan application {AppId} rejected by {ReviewedBy}", loanApplicationId, dto.ReviewedBy);
            return true;
        }

        public async Task<PagedResultDto<LoanResponseDto>> GetPendingLoanApplicationsAsync(int pageNumber, int pageSize)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var query = _context.LoanApplications
                .Where(l => l.ApplicationStatus == "Pending")
                .OrderBy(l => l.AppliedDate);

            var totalCount = await query.CountAsync();

            var applications = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = _mapper.Map<IEnumerable<LoanResponseDto>>(applications);
            return new PagedResultDto<LoanResponseDto>(
                data, pageNumber, pageSize, totalCount,
                (int)Math.Ceiling(totalCount / (double)pageSize));
        }
    }
}
