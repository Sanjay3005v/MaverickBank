using MaverickBank.DTOs.Loan;
using MaverickBank.DTOs.Transaction;
using MaverickBank.Services.AuditLog;
using MaverickBank.Services.Loan;
using Microsoft.Extensions.Logging;
using Moq;


namespace MaverickBankTests
{
    [TestFixture]
    public class LoanServiceTests
    {
        private MaverickBank.Data.AppDbContext _ctx = null!;
        private Mock<IAuditLogService> _audit = null!;
        private Mock<ILogger<LoanService>> _logger = null!;
        private LoanService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _ctx = TestHelpers.CreateDbContext();
            _audit = new Mock<IAuditLogService>();
            _logger = new Mock<ILogger<LoanService>>();
            _sut = new LoanService(_ctx, TestHelpers.CreateMapper(), _audit.Object, _logger.Object);
        }

        [TearDown]
        public void TearDown() => _ctx.Dispose();


        [Test]
        public async Task ApplyLoanAsync_ValidData_CreatesPendingApplication()
        {
            var (user, lt) = SeedUserAndLoanType();
            var result = await _sut.ApplyLoanAsync(new ApplyLoanDto(user.UserId, lt.LoanTypeId, 50_000m, 24, "Home", 60_000m));

            Assert.That(result.LoanStatus, Is.EqualTo("Pending"));
            Assert.That(result.LoanApplicationId, Is.GreaterThan(0));
        }

        [Test]
        public void ApplyLoanAsync_UserNotFound_ThrowsKeyNotFound()
        {
            var lt = TestHelpers.SeedLoanType(_ctx);
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.ApplyLoanAsync(new ApplyLoanDto(99999, lt.LoanTypeId, 50_000m, 24, "X", 60_000m)));
        }

        [Test]
        public void ApplyLoanAsync_LoanTypeNotFound_ThrowsKeyNotFound()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId);
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.ApplyLoanAsync(new ApplyLoanDto(user.UserId, 99999, 50_000m, 24, "X", 60_000m)));
        }

        [Test]
        public void ApplyLoanAsync_AmountBelowMinimum_ThrowsInvalidOperation()
        {
            var (user, lt) = SeedUserAndLoanType(min: 10_000m, max: 500_000m);
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.ApplyLoanAsync(new ApplyLoanDto(user.UserId, lt.LoanTypeId, 5_000m, 24, "X", 60_000m)));
        }

        [Test]
        public void ApplyLoanAsync_AmountAboveMaximum_ThrowsInvalidOperation()
        {
            var (user, lt) = SeedUserAndLoanType(min: 10_000m, max: 500_000m);
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.ApplyLoanAsync(new ApplyLoanDto(user.UserId, lt.LoanTypeId, 600_000m, 24, "X", 60_000m)));
        }

        [Test]
        public void ApplyLoanAsync_TenureBelowMinimum_ThrowsInvalidOperation()
        {
            var (user, lt) = SeedUserAndLoanType(minTenure: 12, maxTenure: 60);
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.ApplyLoanAsync(new ApplyLoanDto(user.UserId, lt.LoanTypeId, 50_000m, 6, "X", 60_000m)));
        }

        [Test]
        public void ApplyLoanAsync_TenureAboveMaximum_ThrowsInvalidOperation()
        {
            var (user, lt) = SeedUserAndLoanType(minTenure: 12, maxTenure: 60);
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.ApplyLoanAsync(new ApplyLoanDto(user.UserId, lt.LoanTypeId, 50_000m, 120, "X", 60_000m)));
        }


        [Test]
        public async Task UpdateLoanStatusAsync_PendingApp_ApprovesAndCreatesLoan()
        {
            var (user, account, app) = SeedApplication();
            var dto = new ApproveLoanDto(50_000m, 10m, 24, "Approved", user.UserId);

            var result = await _sut.UpdateLoanStatusAsync((int)app.LoanApplicationId, dto);

            Assert.That(result, Is.True);
            Assert.That(_ctx.LoanApplications.Find(app.LoanApplicationId)!.ApplicationStatus, Is.EqualTo("Approved"));
            Assert.That(_ctx.Loans.Any(l => l.LoanApplicationId == app.LoanApplicationId), Is.True);
            Assert.That(_ctx.Accounts.Find(account.AccountId)!.Balance, Is.EqualTo(50_000m));
        }

        [Test]
        public void UpdateLoanStatusAsync_AlreadyApproved_ThrowsInvalidOperation()
        {
            var (user, _, app) = SeedApplication(appStatus: "Approved");
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.UpdateLoanStatusAsync((int)app.LoanApplicationId,
                    new ApproveLoanDto(50_000m, 10m, 24, "", user.UserId)));
        }

        [Test]
        public async Task UpdateLoanStatusAsync_NotFound_ReturnsFalse()
        {
            Assert.That(await _sut.UpdateLoanStatusAsync(99999, new ApproveLoanDto(1m, 10m, 12, "", 1)), Is.False);
        }


        [Test]
        public async Task RejectLoanAsync_PendingApp_SetsRejected()
        {
            var (user, _, app) = SeedApplication();
            var result = await _sut.RejectLoanAsync((int)app.LoanApplicationId, new RejectLoanDto(user.UserId, "Low score"));

            Assert.That(result, Is.True);
            Assert.That(_ctx.LoanApplications.Find(app.LoanApplicationId)!.ApplicationStatus, Is.EqualTo("Rejected"));
        }

        [Test]
        public void RejectLoanAsync_AlreadyRejected_ThrowsInvalidOperation()
        {
            var (user, _, app) = SeedApplication(appStatus: "Rejected");
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.RejectLoanAsync((int)app.LoanApplicationId, new RejectLoanDto(user.UserId, "X")));
        }

        [Test]
        public async Task RejectLoanAsync_NotFound_ReturnsFalse()
        {
            Assert.That(await _sut.RejectLoanAsync(99999, new RejectLoanDto(1, "X")), Is.False);
        }


        [Test]
        public async Task RepayLoanAsync_ValidAmount_DeductsOutstanding()
        {
            var loan = SeedActiveLoan(50_000m);
            var before = loan.OutstandingAmount;

            var result = await _sut.RepayLoanAsync(new LoanRepaymentDto(loan.LoanId, 5_000m, "Online", null));

            Assert.That(result, Is.True);
            Assert.That(_ctx.Loans.Find(loan.LoanId)!.OutstandingAmount, Is.EqualTo(before - 5_000m));
        }

        [Test]
        public async Task RepayLoanAsync_FullRepayment_ClosesLoan()
        {
            var loan = SeedActiveLoan(10_000m);

            await _sut.RepayLoanAsync(new LoanRepaymentDto(loan.LoanId, loan.OutstandingAmount, "Cash", null));

            Assert.That(_ctx.Loans.Find(loan.LoanId)!.LoanStatus, Is.EqualTo("Closed"));
        }

        [Test]
        public void RepayLoanAsync_AmountExceedsOutstanding_ThrowsException()
        {
            var loan = SeedActiveLoan(10_000m);
            Assert.ThrowsAsync<Exception>(
                () => _sut.RepayLoanAsync(new LoanRepaymentDto(loan.LoanId, 15_000m, "Cash", null)));
        }

        [Test]
        public void RepayLoanAsync_ZeroAmount_ThrowsException()
        {
            var loan = SeedActiveLoan(10_000m);
            Assert.ThrowsAsync<Exception>(
                () => _sut.RepayLoanAsync(new LoanRepaymentDto(loan.LoanId, 0m, "Cash", null)));
        }

        [Test]
        public async Task RepayLoanAsync_LoanNotFound_ReturnsFalse()
        {
            Assert.That(await _sut.RepayLoanAsync(new LoanRepaymentDto(99999, 100m, "Cash", null)), Is.False);
        }


        [Test]
        public async Task GetPendingLoanApplicationsAsync_ReturnsPendingOnly()
        {
            SeedApplication("Pending");
            SeedApplication("Approved");
            SeedApplication("Rejected");

            var result = await _sut.GetPendingLoanApplicationsAsync(1, 10);

            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(result.Data.All(d => d.LoanStatus == "Pending"), Is.True);
        }

        [Test]
        public async Task GetPendingLoanApplicationsAsync_ReturnsPagedResult()
        {
            for (int i = 0; i < 12; i++)
                SeedApplication("Pending");

            var page1 = await _sut.GetPendingLoanApplicationsAsync(1, 10);
            var page2 = await _sut.GetPendingLoanApplicationsAsync(2, 10);

            Assert.That(page1.Data.Count(), Is.EqualTo(10));
            Assert.That(page2.Data.Count(), Is.EqualTo(2));
        }


        [Test]
        public async Task GetLoansByUserIdAsync_ReturnsOnlyThatUsersLoans()
        {
            var (user, account, app) = SeedApplication();
            await _sut.UpdateLoanStatusAsync((int)app.LoanApplicationId,
                new ApproveLoanDto(30_000m, 10m, 12, "OK", 1));

            var result = await _sut.GetLoansByUserIdAsync(user.UserId, 1, 10);

            Assert.That(result.TotalCount, Is.EqualTo(1));
        }


        [Test]
        public async Task GetLoanByIdAsync_Existing_ReturnsLoan()
        {
            var loan = SeedActiveLoan(20_000m);
            var result = await _sut.GetLoanByIdAsync((int)loan.LoanId);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.LoanId, Is.EqualTo(loan.LoanId));
        }

        [Test]
        public async Task GetLoanByIdAsync_NotFound_ReturnsNull()
        {
            Assert.That(await _sut.GetLoanByIdAsync(99999), Is.Null);
        }


        private (Models.User user, Models.LoanType lt) SeedUserAndLoanType(
            decimal min = 10_000m, decimal max = 500_000m,
            int minTenure = 12, int maxTenure = 60)
        {
            var role = TestHelpers.SeedRole(_ctx, "Customer");
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, $"{Guid.NewGuid():N}@bank.com");
            var lt = TestHelpers.SeedLoanType(_ctx, $"LT{Guid.NewGuid():N}", 10m, min, max, minTenure, maxTenure);
            return (user, lt);
        }

        private (Models.User user, Models.Account account, Models.LoanApplication app)
            SeedApplication(string appStatus = "Pending")
        {
            var role = TestHelpers.SeedRole(_ctx, "Customer");
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, $"{Guid.NewGuid():N}@bank.com");
            var branch = TestHelpers.SeedBranch(_ctx, $"MV{Guid.NewGuid():N}"[..11]);
            var at = TestHelpers.SeedAccountType(_ctx, $"S{Guid.NewGuid():N}");
            var acct = TestHelpers.SeedAccount(_ctx, user.UserId, branch.BranchId, at.AccountTypeId, 0m);
            var lt = TestHelpers.SeedLoanType(_ctx, $"LT{Guid.NewGuid():N}");
            var app = TestHelpers.SeedLoanApplication(_ctx, user.UserId, lt.LoanTypeId, appStatus);
            return (user, acct, app);
        }

        private Models.Loan SeedActiveLoan(decimal amount)
        {
            var (_, account, app) = SeedApplication("Approved");
            return TestHelpers.SeedLoan(_ctx, app.LoanApplicationId, account.AccountId, amount);
        }
    }
}
