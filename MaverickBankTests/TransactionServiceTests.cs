using MaverickBank.DTOs.Transaction;
using MaverickBank.Services.AuditLog;
using MaverickBank.Services.Transaction;
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
    public class TransactionServiceTests
    {
        private MaverickBank.Data.AppDbContext _ctx = null!;
        private Mock<IAuditLogService> _audit = null!;
        private Mock<ILogger<TransactionService>> _logger = null!;
        private TransactionService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _ctx = TestHelpers.CreateDbContext();
            _audit = new Mock<IAuditLogService>();
            _logger = new Mock<ILogger<TransactionService>>();
            _sut = new TransactionService(_ctx, TestHelpers.CreateMapper(), _audit.Object, _logger.Object);

            TestHelpers.SeedTransactionType(_ctx, "Deposit");
            TestHelpers.SeedTransactionType(_ctx, "Withdrawal");
            TestHelpers.SeedTransactionType(_ctx, "Transfer");
        }

        [TearDown]
        public void TearDown() => _ctx.Dispose();

        [Test]
        public async Task DepositAsync_ValidAmount_IncreasesBalance()
        {
            var account = MakeAccount(1_000m);
            var result = await _sut.DepositAsync(new DepositDto(account.AccountId, 500m, "Salary"));

            Assert.That(result.TransactionStatus, Is.EqualTo("Success"));
            Assert.That(result.Amount, Is.EqualTo(500m));
            Assert.That(_ctx.Accounts.Find(account.AccountId)!.Balance, Is.EqualTo(1_500m));
        }

        [Test]
        public void DepositAsync_ZeroAmount_ThrowsInvalidOperation()
        {
            var account = MakeAccount();
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.DepositAsync(new DepositDto(account.AccountId, 0m, "")));
        }

        [Test]
        public void DepositAsync_NegativeAmount_ThrowsInvalidOperation()
        {
            var account = MakeAccount();
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.DepositAsync(new DepositDto(account.AccountId, -1m, "")));
        }

        [Test]
        public void DepositAsync_ClosedAccount_ThrowsInvalidOperation()
        {
            var account = MakeAccount(status: "Closed");
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.DepositAsync(new DepositDto(account.AccountId, 100m, "")));
        }

        [Test]
        public void DepositAsync_AccountNotFound_ThrowsKeyNotFound()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.DepositAsync(new DepositDto(99999, 100m, "")));
        }

        [Test]
        public async Task DepositAsync_CreatesTransactionRecord()
        {
            var account = MakeAccount();
            await _sut.DepositAsync(new DepositDto(account.AccountId, 200m, "Test"));

            Assert.That(_ctx.Transactions.Any(t => t.ToAccountId == account.AccountId && t.Amount == 200m), Is.True);
        }


        [Test]
        public async Task WithdrawAsync_SufficientBalance_DeductsAmount()
        {
            var account = MakeAccount(5_000m);
            var result = await _sut.WithdrawAsync(new WithdrawDto(account.AccountId, 1_000m, "ATM"));

            Assert.That(result.TransactionStatus, Is.EqualTo("Success"));
            Assert.That(_ctx.Accounts.Find(account.AccountId)!.Balance, Is.EqualTo(4_000m));
        }

        [Test]
        public void WithdrawAsync_InsufficientBalance_ThrowsInvalidOperation()
        {
            var account = MakeAccount(100m);
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.WithdrawAsync(new WithdrawDto(account.AccountId, 500m, "")));
        }

        [Test]
        public void WithdrawAsync_ZeroAmount_ThrowsInvalidOperation()
        {
            var account = MakeAccount();
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.WithdrawAsync(new WithdrawDto(account.AccountId, 0m, "")));
        }

        [Test]
        public void WithdrawAsync_ClosedAccount_ThrowsInvalidOperation()
        {
            var account = MakeAccount(5_000m, status: "Closed");
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.WithdrawAsync(new WithdrawDto(account.AccountId, 100m, "")));
        }

        [Test]
        public void WithdrawAsync_AccountNotFound_ThrowsKeyNotFound()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.WithdrawAsync(new WithdrawDto(99999, 100m, "")));
        }


        [Test]
        public async Task TransferAsync_ValidTransfer_UpdatesBothAccounts()
        {
            var from = MakeAccount(10_000m);
            var to = MakeAccount(2_000m);

            var result = await _sut.TransferAsync(new TransferDto(from.AccountId, to.AccountId, 3_000m, "Rent"));

            Assert.That(result.TransactionStatus, Is.EqualTo("Success"));
            Assert.That(_ctx.Accounts.Find(from.AccountId)!.Balance, Is.EqualTo(7_000m));
            Assert.That(_ctx.Accounts.Find(to.AccountId)!.Balance, Is.EqualTo(5_000m));
        }

        [Test]
        public void TransferAsync_SameAccount_ThrowsInvalidOperation()
        {
            var account = MakeAccount(5_000m);
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.TransferAsync(new TransferDto(account.AccountId, account.AccountId, 100m, "")));
        }

        [Test]
        public void TransferAsync_InsufficientBalance_ThrowsException()
        {
            var from = MakeAccount(100m);
            var to = MakeAccount(0m);
            Assert.ThrowsAsync<Exception>(
                () => _sut.TransferAsync(new TransferDto(from.AccountId, to.AccountId, 500m, "")));
        }

        [Test]
        public void TransferAsync_SourceClosed_ThrowsInvalidOperation()
        {
            var from = MakeAccount(5_000m, status: "Closed");
            var to = MakeAccount(0m);
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.TransferAsync(new TransferDto(from.AccountId, to.AccountId, 100m, "")));
        }

        [Test]
        public void TransferAsync_DestinationClosed_ThrowsInvalidOperation()
        {
            var from = MakeAccount(5_000m);
            var to = MakeAccount(0m, status: "Closed");
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.TransferAsync(new TransferDto(from.AccountId, to.AccountId, 100m, "")));
        }

        [Test]
        public void TransferAsync_DestinationNotFound_ThrowsKeyNotFound()
        {
            var from = MakeAccount(5_000m);
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.TransferAsync(new TransferDto(from.AccountId, 99999L, 100m, "")));
        }


        [Test]
        public async Task GetTransactionsByAccountIdAsync_NoFilter_ReturnsPaged()
        {
            var account = MakeAccount(50_000m);
            for (int i = 0; i < 5; i++)
                await _sut.DepositAsync(new DepositDto(account.AccountId, 100m, $"d{i}"));

            var result = await _sut.GetTransactionsByAccountIdAsync(account.AccountId, null, null, null, 1, 10);

            Assert.That(result.TotalCount, Is.EqualTo(5));
        }

        [Test]
        public async Task GetTransactionsByAccountIdAsync_Last10Filter_CapsAt10()
        {
            var account = MakeAccount(50_000m);
            for (int i = 0; i < 12; i++)
                await _sut.DepositAsync(new DepositDto(account.AccountId, 50m, $"d{i}"));

            var result = await _sut.GetTransactionsByAccountIdAsync(account.AccountId, "last10");

            Assert.That(result.Data.Count(), Is.EqualTo(10));
        }

        [Test]
        public async Task GetTransactionsByAccountIdAsync_DateRangeFilter_ReturnsWithinRange()
        {
            var account = MakeAccount(50_000m);
            await _sut.DepositAsync(new DepositDto(account.AccountId, 100m, "in-range"));

            var from = DateTime.UtcNow.AddMinutes(-1);
            var to = DateTime.UtcNow.AddMinutes(1);
            var result = await _sut.GetTransactionsByAccountIdAsync(account.AccountId, "daterange", from, to);

            Assert.That(result.TotalCount, Is.GreaterThan(0));
        }

        [Test]
        public async Task GetTransactionsByAccountIdAsync_LastMonthFilter_ReturnsRecentTx()
        {
            var account = MakeAccount(50_000m);
            await _sut.DepositAsync(new DepositDto(account.AccountId, 100m, "recent"));

            var result = await _sut.GetTransactionsByAccountIdAsync(account.AccountId, "lastmonth");

            Assert.That(result.TotalCount, Is.GreaterThan(0));
        }


        [Test]
        public async Task GetTransactionSummaryByAccountIdAsync_CalculatesTotals()
        {
            var account = MakeAccount(50_000m);
            await _sut.DepositAsync(new DepositDto(account.AccountId, 1_000m, "in1"));
            await _sut.DepositAsync(new DepositDto(account.AccountId, 2_000m, "in2"));
            await _sut.WithdrawAsync(new WithdrawDto(account.AccountId, 500m, "out1"));

            var summary = await _sut.GetTransactionSummaryByAccountIdAsync(account.AccountId);

            Assert.That(summary.TotalInbound, Is.EqualTo(3_000m));
            Assert.That(summary.TotalOutbound, Is.EqualTo(500m));
            Assert.That(summary.InboundCount, Is.EqualTo(2));
            Assert.That(summary.OutboundCount, Is.EqualTo(1));
        }


        private MaverickBank.Models.Account MakeAccount(decimal balance = 10_000m, string status = "Active")
        {
            var role = TestHelpers.SeedRole(_ctx, "Customer");
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, $"{Guid.NewGuid():N}@bank.com");
            var branch = TestHelpers.SeedBranch(_ctx, $"MV{Guid.NewGuid():N}"[..11]);
            var at = TestHelpers.SeedAccountType(_ctx, $"S{Guid.NewGuid():N}");
            return TestHelpers.SeedAccount(_ctx, user.UserId, branch.BranchId, at.AccountTypeId, balance, status);
        }
    }
}
