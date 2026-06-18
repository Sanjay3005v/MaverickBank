using MaverickBank.DTOs.AccountOpeningRequestDto;
using MaverickBank.Services.Account;
using MaverickBank.Services.AccountOpeningRequest;
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
    public class AccountOpeningRequestTests
    {
        [TestFixture]
        public class AccountOpeningRequestServiceTests
        {
            private MaverickBank.Data.AppDbContext _ctx = null!;
            private Mock<IAuditLogService> _audit = null!;
            private Mock<ILogger<AccountOpeningRequestService>> _logger = null!;
            private AccountOpeningRequestService _sut = null!;

            [SetUp]
            public void SetUp()
            {
                _ctx = TestHelpers.CreateDbContext();
                _audit = new Mock<IAuditLogService>();
                _logger = new Mock<ILogger<AccountOpeningRequestService>>();

                var accountService = new AccountService(
                    _ctx,
                    TestHelpers.CreateMapper(),
                    _audit.Object,
                    new Mock<ILogger<AccountService>>().Object);

                _sut = new AccountOpeningRequestService(_ctx, accountService, _audit.Object, _logger.Object);
            }

            [TearDown]
            public void TearDown() => _ctx.Dispose();


            [Test]
            public async Task CreateRequestAsync_ValidData_CreatesPendingRequest()
            {
                var (user, branch, at) = Seed();
                var result = await _sut.CreateRequestAsync(
                    new CreateAccountOpeningRequestDto(user.UserId, branch.BranchId, at.AccountTypeId, 1_000m));

                Assert.That(result.Status, Is.EqualTo("Pending"));
                Assert.That(result.UserId, Is.EqualTo(user.UserId));
                Assert.That(result.BranchId, Is.EqualTo(branch.BranchId));
                Assert.That(result.AccountTypeId, Is.EqualTo(at.AccountTypeId));
                Assert.That(result.InitialDeposit, Is.EqualTo(1_000m));
                Assert.That(result.RequestId, Is.GreaterThan(0));
            }

            [Test]
            public void CreateRequestAsync_UserNotFound_ThrowsKeyNotFound()
            {
                var branch = TestHelpers.SeedBranch(_ctx);
                var at = TestHelpers.SeedAccountType(_ctx);

                Assert.ThrowsAsync<KeyNotFoundException>(() =>
                    _sut.CreateRequestAsync(new CreateAccountOpeningRequestDto(99999, branch.BranchId, at.AccountTypeId, 0m)));
            }

            [Test]
            public void CreateRequestAsync_BranchNotFound_ThrowsKeyNotFound()
            {
                var role = TestHelpers.SeedRole(_ctx);
                var user = TestHelpers.SeedUser(_ctx, role.RoleId);
                var at = TestHelpers.SeedAccountType(_ctx);

                Assert.ThrowsAsync<KeyNotFoundException>(() =>
                    _sut.CreateRequestAsync(new CreateAccountOpeningRequestDto(user.UserId, 99999, at.AccountTypeId, 0m)));
            }

            [Test]
            public void CreateRequestAsync_AccountTypeNotFound_ThrowsKeyNotFound()
            {
                var role = TestHelpers.SeedRole(_ctx);
                var user = TestHelpers.SeedUser(_ctx, role.RoleId);
                var branch = TestHelpers.SeedBranch(_ctx);

                Assert.ThrowsAsync<KeyNotFoundException>(() =>
                    _sut.CreateRequestAsync(new CreateAccountOpeningRequestDto(user.UserId, branch.BranchId, 99999, 0m)));
            }

            [Test]
            public void CreateRequestAsync_NegativeDeposit_ThrowsInvalidOperation()
            {
                var (user, branch, at) = Seed();

                Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _sut.CreateRequestAsync(new CreateAccountOpeningRequestDto(user.UserId, branch.BranchId, at.AccountTypeId, -100m)));
            }

            [Test]
            public void CreateRequestAsync_DuplicatePendingRequest_ThrowsInvalidOperation()
            {
                var (user, branch, at) = Seed();
                _ctx.AccountOpeningRequests.Add(new MaverickBank.Models.AccountOpeningRequest
                {
                    UserId = user.UserId,
                    BranchId = branch.BranchId,
                    AccountTypeId = at.AccountTypeId,
                    InitialDeposit = 500m,
                    RequestDate = DateTime.UtcNow,
                    Status = "Pending"
                });
                _ctx.SaveChanges();

                Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _sut.CreateRequestAsync(new CreateAccountOpeningRequestDto(user.UserId, branch.BranchId, at.AccountTypeId, 500m)));
            }


            [Test]
            public async Task GetPendingRequestsAsync_ReturnsOnlyPendingRequests()
            {
                var (user, branch, at) = Seed();
                _ctx.AccountOpeningRequests.Add(new MaverickBank.Models.AccountOpeningRequest
                { UserId = user.UserId, BranchId = branch.BranchId, AccountTypeId = at.AccountTypeId, InitialDeposit = 0m, RequestDate = DateTime.UtcNow, Status = "Pending" });

                var at2 = TestHelpers.SeedAccountType(_ctx, $"S{Guid.NewGuid():N}");
                _ctx.AccountOpeningRequests.Add(new MaverickBank.Models.AccountOpeningRequest
                { UserId = user.UserId, BranchId = branch.BranchId, AccountTypeId = at2.AccountTypeId, InitialDeposit = 0m, RequestDate = DateTime.UtcNow, Status = "Approved" });

                var at3 = TestHelpers.SeedAccountType(_ctx, $"S{Guid.NewGuid():N}");
                _ctx.AccountOpeningRequests.Add(new MaverickBank.Models.AccountOpeningRequest
                { UserId = user.UserId, BranchId = branch.BranchId, AccountTypeId = at3.AccountTypeId, InitialDeposit = 0m, RequestDate = DateTime.UtcNow, Status = "Rejected" });
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
                    var (user, branch, at) = Seed();
                    _ctx.AccountOpeningRequests.Add(new MaverickBank.Models.AccountOpeningRequest
                    { UserId = user.UserId, BranchId = branch.BranchId, AccountTypeId = at.AccountTypeId, InitialDeposit = 0m, RequestDate = DateTime.UtcNow, Status = "Pending" });
                }
                _ctx.SaveChanges();

                var page1 = await _sut.GetPendingRequestsAsync(1, 10);
                var page2 = await _sut.GetPendingRequestsAsync(2, 10);

                Assert.That(page1.Data.Count(), Is.EqualTo(10));
                Assert.That(page2.Data.Count(), Is.EqualTo(2));
                Assert.That(page1.TotalPages, Is.EqualTo(2));
            }


            [Test]
            public async Task GetRequestsByUserIdAsync_ReturnsOnlyThatUsersRequests()
            {
                var (u1, branch1, at1) = Seed();
                var (u2, branch2, at2) = Seed();

                _ctx.AccountOpeningRequests.Add(new MaverickBank.Models.AccountOpeningRequest
                { UserId = u1.UserId, BranchId = branch1.BranchId, AccountTypeId = at1.AccountTypeId, InitialDeposit = 0m, RequestDate = DateTime.UtcNow, Status = "Pending" });
                _ctx.AccountOpeningRequests.Add(new MaverickBank.Models.AccountOpeningRequest
                { UserId = u2.UserId, BranchId = branch2.BranchId, AccountTypeId = at2.AccountTypeId, InitialDeposit = 0m, RequestDate = DateTime.UtcNow, Status = "Pending" });
                _ctx.SaveChanges();

                var result = await _sut.GetRequestsByUserIdAsync(u1.UserId, 1, 10);

                Assert.That(result.TotalCount, Is.EqualTo(1));
                Assert.That(result.Data.All(r => r.UserId == u1.UserId), Is.True);
            }

            [Test]
            public async Task GetRequestsByUserIdAsync_ReturnsPagedResult()
            {
                var (user, branch, _) = Seed();
                for (int i = 0; i < 6; i++)
                {
                    var atN = TestHelpers.SeedAccountType(_ctx, $"S{Guid.NewGuid():N}");
                    _ctx.AccountOpeningRequests.Add(new MaverickBank.Models.AccountOpeningRequest
                    { UserId = user.UserId, BranchId = branch.BranchId, AccountTypeId = atN.AccountTypeId, InitialDeposit = 0m, RequestDate = DateTime.UtcNow, Status = "Pending" });
                }
                _ctx.SaveChanges();

                var result = await _sut.GetRequestsByUserIdAsync(user.UserId, 1, 4);

                Assert.That(result.TotalCount, Is.EqualTo(6));
                Assert.That(result.Data.Count(), Is.EqualTo(4));
                Assert.That(result.TotalPages, Is.EqualTo(2));
            }


            [Test]
            public async Task ApproveRequestAsync_PendingRequest_CreatesAccountAndSetsApproved()
            {
                var (user, branch, at) = Seed();
                var req = new MaverickBank.Models.AccountOpeningRequest
                {
                    UserId = user.UserId,
                    BranchId = branch.BranchId,
                    AccountTypeId = at.AccountTypeId,
                    InitialDeposit = 2_000m,
                    RequestDate = DateTime.UtcNow,
                    Status = "Pending"
                };
                _ctx.AccountOpeningRequests.Add(req);
                _ctx.SaveChanges();

                var result = await _sut.ApproveRequestAsync(req.RequestId, user.UserId, "Looks good");

                Assert.That(result, Is.True);
                var updated = _ctx.AccountOpeningRequests.Find(req.RequestId)!;
                Assert.That(updated.Status, Is.EqualTo("Approved"));
                Assert.That(updated.ReviewedBy, Is.EqualTo(user.UserId));
                Assert.That(updated.ReviewedDate, Is.Not.Null);
                Assert.That(updated.CreatedAccountId, Is.Not.Null);

                var account = _ctx.Accounts.Find(updated.CreatedAccountId!.Value)!;
                Assert.That(account.Balance, Is.EqualTo(2_000m));
                Assert.That(account.Status, Is.EqualTo("Active"));
            }

            [Test]
            public async Task ApproveRequestAsync_NotFound_ReturnsFalse()
            {
                Assert.That(await _sut.ApproveRequestAsync(99999, 1, ""), Is.False);
            }

            [Test]
            public void ApproveRequestAsync_AlreadyApproved_ThrowsInvalidOperation()
            {
                var (user, branch, at) = Seed();
                var req = new MaverickBank.Models.AccountOpeningRequest
                {
                    UserId = user.UserId,
                    BranchId = branch.BranchId,
                    AccountTypeId = at.AccountTypeId,
                    InitialDeposit = 0m,
                    RequestDate = DateTime.UtcNow,
                    Status = "Approved"
                };
                _ctx.AccountOpeningRequests.Add(req);
                _ctx.SaveChanges();

                Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _sut.ApproveRequestAsync(req.RequestId, user.UserId, ""));
            }

            [Test]
            public void ApproveRequestAsync_AlreadyRejected_ThrowsInvalidOperation()
            {
                var (user, branch, at) = Seed();
                var req = new MaverickBank.Models.AccountOpeningRequest
                {
                    UserId = user.UserId,
                    BranchId = branch.BranchId,
                    AccountTypeId = at.AccountTypeId,
                    InitialDeposit = 0m,
                    RequestDate = DateTime.UtcNow,
                    Status = "Rejected"
                };
                _ctx.AccountOpeningRequests.Add(req);
                _ctx.SaveChanges();

                Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _sut.ApproveRequestAsync(req.RequestId, user.UserId, ""));
            }


            [Test]
            public async Task RejectRequestAsync_PendingRequest_SetsRejected()
            {
                var (user, branch, at) = Seed();
                var req = new MaverickBank.Models.AccountOpeningRequest
                {
                    UserId = user.UserId,
                    BranchId = branch.BranchId,
                    AccountTypeId = at.AccountTypeId,
                    InitialDeposit = 0m,
                    RequestDate = DateTime.UtcNow,
                    Status = "Pending"
                };
                _ctx.AccountOpeningRequests.Add(req);
                _ctx.SaveChanges();

                var result = await _sut.RejectRequestAsync(req.RequestId, user.UserId, "Incomplete docs");

                Assert.That(result, Is.True);
                var updated = _ctx.AccountOpeningRequests.Find(req.RequestId)!;
                Assert.That(updated.Status, Is.EqualTo("Rejected"));
                Assert.That(updated.ReviewedBy, Is.EqualTo(user.UserId));
                Assert.That(updated.Remarks, Is.EqualTo("Incomplete docs"));
            }

            [Test]
            public async Task RejectRequestAsync_NotFound_ReturnsFalse()
            {
                Assert.That(await _sut.RejectRequestAsync(99999, 1, ""), Is.False);
            }

            [Test]
            public void RejectRequestAsync_AlreadyRejected_ThrowsInvalidOperation()
            {
                var (user, branch, at) = Seed();
                var req = new MaverickBank.Models.AccountOpeningRequest
                {
                    UserId = user.UserId,
                    BranchId = branch.BranchId,
                    AccountTypeId = at.AccountTypeId,
                    InitialDeposit = 0m,
                    RequestDate = DateTime.UtcNow,
                    Status = "Rejected"
                };
                _ctx.AccountOpeningRequests.Add(req);
                _ctx.SaveChanges();

                Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _sut.RejectRequestAsync(req.RequestId, user.UserId, ""));
            }

            [Test]
            public void RejectRequestAsync_AlreadyApproved_ThrowsInvalidOperation()
            {
                var (user, branch, at) = Seed();
                var req = new MaverickBank.Models.AccountOpeningRequest
                {
                    UserId = user.UserId,
                    BranchId = branch.BranchId,
                    AccountTypeId = at.AccountTypeId,
                    InitialDeposit = 0m,
                    RequestDate = DateTime.UtcNow,
                    Status = "Approved"
                };
                _ctx.AccountOpeningRequests.Add(req);
                _ctx.SaveChanges();

                Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _sut.RejectRequestAsync(req.RequestId, user.UserId, ""));
            }


            private (MaverickBank.Models.User, MaverickBank.Models.Branch, MaverickBank.Models.AccountType) Seed()
            {
                var role = TestHelpers.SeedRole(_ctx, "Customer");
                var user = TestHelpers.SeedUser(_ctx, role.RoleId, $"{Guid.NewGuid():N}@bank.com");
                var branch = TestHelpers.SeedBranch(_ctx, $"MV{Guid.NewGuid():N}"[..11]);
                var at = TestHelpers.SeedAccountType(_ctx, $"S{Guid.NewGuid():N}");
                return (user, branch, at);
            }
        }
    }
}
