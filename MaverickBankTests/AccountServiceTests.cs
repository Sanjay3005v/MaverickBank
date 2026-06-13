using MaverickBank.DTOs.Account;
using MaverickBank.Services.Account;
using MaverickBank.Services.AuditLog;
using Microsoft.Extensions.Logging;
using Moq;


namespace MaverickBankTests
{
    [TestFixture]
    public class AccountServiceTests
    {
        private MaverickBank.Data.AppDbContext _ctx = null!;
        private Mock<IAuditLogService> _audit = null!;
        private Mock<ILogger<AccountService>> _logger = null!;
        private AccountService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _ctx = TestHelpers.CreateDbContext();
            _audit = new Mock<IAuditLogService>();
            _logger = new Mock<ILogger<AccountService>>();
            _sut = new AccountService(_ctx, TestHelpers.CreateMapper(), _audit.Object, _logger.Object);
        }

        [TearDown]
        public void TearDown() => _ctx.Dispose();


        [Test]
        public async Task CreateAccountAsync_ValidData_ReturnsActiveAccount()
        {
            var (user, branch, at) = Seed();
            var result = await _sut.CreateAccountAsync(new CreateAccountDto(user.UserId, branch.BranchId, at.AccountTypeId, 5_000m));

            Assert.That(result.Status, Is.EqualTo("Active"));
            Assert.That(result.Balance, Is.EqualTo(5_000m));
            Assert.That(result.AccountNumber, Has.Length.GreaterThan(0));
            Assert.That(result.UserId, Is.EqualTo(user.UserId));
        }

        [Test]
        public void CreateAccountAsync_UserNotFound_ThrowsKeyNotFound()
        {
            var branch = TestHelpers.SeedBranch(_ctx);
            var at = TestHelpers.SeedAccountType(_ctx);

            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.CreateAccountAsync(new CreateAccountDto(99999, branch.BranchId, at.AccountTypeId, 0m)));
        }

        [Test]
        public void CreateAccountAsync_BranchNotFound_ThrowsKeyNotFound()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId);
            var at = TestHelpers.SeedAccountType(_ctx);

            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.CreateAccountAsync(new CreateAccountDto(user.UserId, 99999, at.AccountTypeId, 0m)));
        }

        [Test]
        public void CreateAccountAsync_AccountTypeNotFound_ThrowsKeyNotFound()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId);
            var branch = TestHelpers.SeedBranch(_ctx);

            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.CreateAccountAsync(new CreateAccountDto(user.UserId, branch.BranchId, 99999, 0m)));
        }

        [Test]
        public void CreateAccountAsync_NegativeDeposit_ThrowsInvalidOperation()
        {
            var (user, branch, at) = Seed("neg");
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.CreateAccountAsync(new CreateAccountDto(user.UserId, branch.BranchId, at.AccountTypeId, -100m)));
        }

        [Test]
        public async Task CreateAccountAsync_GeneratesUniqueAccountNumbers()
        {
            var (user, branch, at) = Seed("uniq");
            var tasks = Enumerable.Range(0, 5)
                .Select(_ => _sut.CreateAccountAsync(new CreateAccountDto(user.UserId, branch.BranchId, at.AccountTypeId, 0m)));
            var results = await Task.WhenAll(tasks);

            Assert.That(results.Select(r => r.AccountNumber).Distinct().Count(), Is.EqualTo(5));
        }


        [Test]
        public async Task GetAccountByIdAsync_Existing_ReturnsDto()
        {
            var (user, branch, at) = Seed("get");
            var account = TestHelpers.SeedAccount(_ctx, user.UserId, branch.BranchId, at.AccountTypeId);

            var result = await _sut.GetAccountByIdAsync(account.AccountId);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.AccountId, Is.EqualTo(account.AccountId));
        }

        [Test]
        public async Task GetAccountByIdAsync_NotFound_ReturnsNull()
        {
            Assert.That(await _sut.GetAccountByIdAsync(99999), Is.Null);
        }


        [Test]
        public async Task GetAllAccountsAsync_ReturnsPagedResult()
        {
            var (user, branch, at) = Seed("all");
            for (int i = 0; i < 12; i++)
                TestHelpers.SeedAccount(_ctx, user.UserId, branch.BranchId, at.AccountTypeId);

            var page1 = await _sut.GetAllAccountsAsync(1, 10);
            var page2 = await _sut.GetAllAccountsAsync(2, 10);

            Assert.That(page1.Data.Count(), Is.EqualTo(10));
            Assert.That(page2.Data.Count(), Is.EqualTo(2));
            Assert.That(page1.TotalCount, Is.EqualTo(12));
            Assert.That(page1.TotalPages, Is.EqualTo(2));
        }


        [Test]
        public async Task GetAccountsByUserIdAsync_ReturnsOnlyThatUsersAccounts()
        {
            var (u1, branch, at) = Seed("byUser");
            var role2 = TestHelpers.SeedRole(_ctx, "Employee");
            var u2 = TestHelpers.SeedUser(_ctx, role2.RoleId, "other@bank.com");

            TestHelpers.SeedAccount(_ctx, u1.UserId, branch.BranchId, at.AccountTypeId);
            TestHelpers.SeedAccount(_ctx, u1.UserId, branch.BranchId, at.AccountTypeId);
            TestHelpers.SeedAccount(_ctx, u2.UserId, branch.BranchId, at.AccountTypeId);

            var result = await _sut.GetAccountsByUserIdAsync(u1.UserId, 1, 10);

            Assert.That(result.TotalCount, Is.EqualTo(2));
            Assert.That(result.Data.All(a => a.UserId == u1.UserId), Is.True);
        }


        [Test]
        public async Task UpdateAccountStatusAsync_ValidAccount_UpdatesStatus()
        {
            var (user, branch, at) = Seed("upd");
            var account = TestHelpers.SeedAccount(_ctx, user.UserId, branch.BranchId, at.AccountTypeId);

            var result = await _sut.UpdateAccountStatusAsync(account.AccountId, new UpdateAccountDto("Frozen"), user.UserId);

            Assert.That(result, Is.True);
            Assert.That(_ctx.Accounts.Find(account.AccountId)!.Status, Is.EqualTo("Frozen"));
        }

        [Test]
        public async Task UpdateAccountStatusAsync_NotFound_ReturnsFalse()
        {
            Assert.That(await _sut.UpdateAccountStatusAsync(99999, new UpdateAccountDto("Active"), 1), Is.False);
        }


        [Test]
        public async Task CloseAccountAsync_ActiveAccount_ClosesIt()
        {
            var (user, branch, at) = Seed("close");
            var account = TestHelpers.SeedAccount(_ctx, user.UserId, branch.BranchId, at.AccountTypeId);

            var result = await _sut.CloseAccountAsync(account.AccountId, new CloseAccountDto("Closing"), user.UserId);

            Assert.That(result, Is.True);
            var closed = _ctx.Accounts.Find(account.AccountId)!;
            Assert.That(closed.Status, Is.EqualTo("Closed"));
            Assert.That(closed.ClosedDate, Is.Not.Null);
        }

        [Test]
        public void CloseAccountAsync_AlreadyClosed_ThrowsInvalidOperation()
        {
            var (user, branch, at) = Seed("closeDup");
            var account = TestHelpers.SeedAccount(_ctx, user.UserId, branch.BranchId, at.AccountTypeId, status: "Closed");

            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.CloseAccountAsync(account.AccountId, new CloseAccountDto(""), user.UserId));
        }

        [Test]
        public async Task CloseAccountAsync_NotFound_ReturnsFalse()
        {
            Assert.That(await _sut.CloseAccountAsync(99999, new CloseAccountDto(""), 1), Is.False);
        }


        private (MaverickBank.Models.User, MaverickBank.Models.Branch, MaverickBank.Models.AccountType) Seed(string suffix = "")
        {
            var role = TestHelpers.SeedRole(_ctx, "Customer");
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, $"acc{suffix}{Guid.NewGuid():N}@bank.com");
            var branch = TestHelpers.SeedBranch(_ctx, $"MV{Guid.NewGuid():N}"[..11]);
            var at = TestHelpers.SeedAccountType(_ctx, $"Sav{Guid.NewGuid():N}");
            return (user, branch, at);
        }
    }
}
