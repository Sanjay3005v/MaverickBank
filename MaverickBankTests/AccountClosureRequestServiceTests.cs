using MaverickBank.DTOs.AccountClosureRequest;
using MaverickBank.Services.AccountClosureRequest;
using MaverickBank.Services.AuditLog;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaverickBankTests
{
    [TestFixture]
    public class AccountClosureRequestServiceTests
    {
        private MaverickBank.Data.AppDbContext _ctx = null!;
        private Mock<IAuditLogService> _audit = null!;
        private Mock<ILogger<AccountClosureRequestService>> _logger = null!;
        private AccountClosureRequestService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _ctx = TestHelpers.CreateDbContext();
            _audit = new Mock<IAuditLogService>();
            _logger = new Mock<ILogger<AccountClosureRequestService>>();
            _sut = new AccountClosureRequestService(_ctx, _audit.Object, _logger.Object);
        }

        [TearDown]
        public void TearDown() => _ctx.Dispose();


        [Test]
        public async Task CreateRequestAsync_ZeroBalance_CreatesPendingRequest()
        {
            var (user, account) = SeedUserAndAccount(balance: 0m);

            var result = await _sut.CreateRequestAsync(
                new CreateAccountClosureRequestDto(account.AccountId, user.UserId));

            Assert.That(result.Status, Is.EqualTo("Pending"));
            Assert.That(result.AccountId, Is.EqualTo(account.AccountId));
            Assert.That(result.RequestedBy, Is.EqualTo(user.UserId));
        }

        [Test]
        public void CreateRequestAsync_NonZeroBalance_ThrowsInvalidOperation()
        {
            var (user, account) = SeedUserAndAccount(balance: 500m);
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.CreateRequestAsync(new CreateAccountClosureRequestDto(account.AccountId, user.UserId)));
        }

        [Test]
        public void CreateRequestAsync_DifferentUser_ThrowsInvalidOperation()
        {
            var (_, account) = SeedUserAndAccount(balance: 0m);
            var role2 = TestHelpers.SeedRole(_ctx, "Customer");
            var other = TestHelpers.SeedUser(_ctx, role2.RoleId, "other@bank.com");

            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.CreateRequestAsync(new CreateAccountClosureRequestDto(account.AccountId, other.UserId)));
        }

        [Test]
        public void CreateRequestAsync_AlreadyClosedAccount_ThrowsInvalidOperation()
        {
            var (user, account) = SeedUserAndAccount(balance: 0m, status: "Closed");
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.CreateRequestAsync(new CreateAccountClosureRequestDto(account.AccountId, user.UserId)));
        }

        [Test]
        public void CreateRequestAsync_DuplicatePendingRequest_ThrowsInvalidOperation()
        {
            var (user, account) = SeedUserAndAccount(balance: 0m);
            _ctx.AccountClosureRequests.Add(new MaverickBank.Models.AccountClosureRequest
            {
                AccountId = account.AccountId,
                RequestedBy = user.UserId,
                RequestDate = DateTime.UtcNow,
                Status = "Pending"
            });
            _ctx.SaveChanges();

            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.CreateRequestAsync(new CreateAccountClosureRequestDto(account.AccountId, user.UserId)));
        }

        [Test]
        public void CreateRequestAsync_AccountNotFound_ThrowsKeyNotFound()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.CreateRequestAsync(new CreateAccountClosureRequestDto(99999, 1)));
        }


        [Test]
        public async Task GetPendingRequestsAsync_ReturnsOnlyPendingRequests()
        {
            var (user, acct) = SeedUserAndAccount(balance: 0m);
            _ctx.AccountClosureRequests.Add(new MaverickBank.Models.AccountClosureRequest
            { AccountId = acct.AccountId, RequestedBy = user.UserId, RequestDate = DateTime.UtcNow, Status = "Pending" });
            _ctx.AccountClosureRequests.Add(new MaverickBank.Models.AccountClosureRequest
            { AccountId = acct.AccountId, RequestedBy = user.UserId, RequestDate = DateTime.UtcNow, Status = "Approved" });
            _ctx.SaveChanges();

            var result = await _sut.GetPendingRequestsAsync(1, 10);

            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(result.Data.All(r => r.Status == "Pending"), Is.True);
        }

        [Test]
        public async Task GetPendingRequestsAsync_ReturnsPagedResult()
        {
            for (int i = 0; i < 12; i++)
            {
                var (user, acct) = SeedUserAndAccount(balance: 0m);
                _ctx.AccountClosureRequests.Add(new MaverickBank.Models.AccountClosureRequest
                { AccountId = acct.AccountId, RequestedBy = user.UserId, RequestDate = DateTime.UtcNow, Status = "Pending" });
            }
            _ctx.SaveChanges();

            var page1 = await _sut.GetPendingRequestsAsync(1, 10);
            var page2 = await _sut.GetPendingRequestsAsync(2, 10);

            Assert.That(page1.Data.Count(), Is.EqualTo(10));
            Assert.That(page2.Data.Count(), Is.EqualTo(2));
        }


        [Test]
        public async Task ApproveRequestAsync_PendingRequest_ClosesAccountAndSetsApproved()
        {
            var (user, account) = SeedUserAndAccount(balance: 0m);
            var req = new MaverickBank.Models.AccountClosureRequest
            {
                AccountId = account.AccountId,
                RequestedBy = user.UserId,
                RequestDate = DateTime.UtcNow,
                Status = "Pending"
            };
            _ctx.AccountClosureRequests.Add(req);
            _ctx.SaveChanges();

            var result = await _sut.ApproveRequestAsync(req.RequestId, user.UserId, "Admin approved");

            Assert.That(result, Is.True);
            Assert.That(_ctx.AccountClosureRequests.Find(req.RequestId)!.Status, Is.EqualTo("Approved"));
            Assert.That(_ctx.Accounts.Find(account.AccountId)!.Status, Is.EqualTo("Closed"));
            Assert.That(_ctx.Accounts.Find(account.AccountId)!.ClosedDate, Is.Not.Null);
        }

        [Test]
        public async Task ApproveRequestAsync_NotFound_ReturnsFalse()
        {
            Assert.That(await _sut.ApproveRequestAsync(99999, 1, ""), Is.False);
        }

        [Test]
        public void ApproveRequestAsync_AlreadyApproved_ThrowsInvalidOperation()
        {
            var (user, account) = SeedUserAndAccount(balance: 0m);
            var req = new MaverickBank.Models.AccountClosureRequest
            {
                AccountId = account.AccountId,
                RequestedBy = user.UserId,
                RequestDate = DateTime.UtcNow,
                Status = "Approved"
            };
            _ctx.AccountClosureRequests.Add(req);
            _ctx.SaveChanges();

            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ApproveRequestAsync(req.RequestId, user.UserId, ""));
        }


        private (MaverickBank.Models.User, MaverickBank.Models.Account) SeedUserAndAccount(decimal balance, string status = "Active")
        {
            var role = TestHelpers.SeedRole(_ctx, "Customer");
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, $"{Guid.NewGuid():N}@bank.com");
            var branch = TestHelpers.SeedBranch(_ctx, $"MV{Guid.NewGuid():N}"[..11]);
            var at = TestHelpers.SeedAccountType(_ctx, $"S{Guid.NewGuid():N}");
            var acct = TestHelpers.SeedAccount(_ctx, user.UserId, branch.BranchId, at.AccountTypeId, balance, status);
            return (user, acct);
        }
    }

}
