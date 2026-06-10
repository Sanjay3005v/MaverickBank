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
    public class AuditLogServiceTests
    {
        private MaverickBank.Data.AppDbContext _ctx = null!;
        private Mock<ILogger<AuditLogService>> _logger = null!;
        private AuditLogService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _ctx = TestHelpers.CreateDbContext();
            _logger = new Mock<ILogger<AuditLogService>>();
            _sut = new AuditLogService(_ctx, TestHelpers.CreateMapper(), _logger.Object);
        }

        [TearDown]
        public void TearDown() => _ctx.Dispose();


        [Test]
        public async Task LogAsync_WritesAuditRecord()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId);

            await _sut.LogAsync(user.UserId, "Test Action", "User", user.UserId, null, "{}");

            var log = _ctx.AuditLogs.Single();
            Assert.That(log.Action, Is.EqualTo("Test Action"));
            Assert.That(log.EntityName, Is.EqualTo("User"));
            Assert.That(log.EntityId, Is.EqualTo(user.UserId));
            Assert.That(log.UserId, Is.EqualTo(user.UserId));
            Assert.That(log.NewValues, Is.EqualTo("{}"));
        }

        [Test]
        public async Task LogAsync_StoresOldAndNewValues()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId);

            await _sut.LogAsync(user.UserId, "Updated", "User", user.UserId, "{\"old\":1}", "{\"new\":2}");

            var log = _ctx.AuditLogs.Single();
            Assert.That(log.OldValues, Is.EqualTo("{\"old\":1}"));
            Assert.That(log.NewValues, Is.EqualTo("{\"new\":2}"));
        }


        [Test]
        public async Task GetAllAsync_ReturnsPagedResult()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId);
            for (int i = 0; i < 5; i++)
                await _sut.LogAsync(user.UserId, $"Action{i}", "Entity", i + 1);

            var page1 = await _sut.GetAllAsync(1, 3);
            var page2 = await _sut.GetAllAsync(2, 3);

            Assert.That(page1.TotalCount, Is.EqualTo(5));
            Assert.That(page1.Data.Count(), Is.EqualTo(3));
            Assert.That(page2.Data.Count(), Is.EqualTo(2));
            Assert.That(page1.TotalPages, Is.EqualTo(2));
        }

        [Test]
        public async Task GetAllAsync_OrderedMostRecentFirst()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId);
            await _sut.LogAsync(user.UserId, "First", "E", 1);
            await Task.Delay(10);
            await _sut.LogAsync(user.UserId, "Second", "E", 2);

            var result = await _sut.GetAllAsync(1, 10);
            var items = result.Data.ToList();

            Assert.That(items[0].Action, Is.EqualTo("Second"));
            Assert.That(items[1].Action, Is.EqualTo("First"));
        }


        [Test]
        public async Task GetByUserIdAsync_ReturnsOnlyThatUsersLogs()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var u1 = TestHelpers.SeedUser(_ctx, role.RoleId, "al1@bank.com");
            var u2 = TestHelpers.SeedUser(_ctx, role.RoleId, "al2@bank.com");

            await _sut.LogAsync(u1.UserId, "A1", "E", 1);
            await _sut.LogAsync(u1.UserId, "A2", "E", 2);
            await _sut.LogAsync(u2.UserId, "A3", "E", 3);

            var result = await _sut.GetByUserIdAsync(u1.UserId, 1, 10);

            Assert.That(result.TotalCount, Is.EqualTo(2));
            Assert.That(result.Data.All(d => d.UserId == u1.UserId), Is.True);
        }

        [Test]
        public async Task GetByUserIdAsync_ReturnsPagedResult()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId);
            for (int i = 0; i < 6; i++)
                await _sut.LogAsync(user.UserId, $"A{i}", "E", i);

            var result = await _sut.GetByUserIdAsync(user.UserId, 1, 4);

            Assert.That(result.TotalCount, Is.EqualTo(6));
            Assert.That(result.Data.Count(), Is.EqualTo(4));
            Assert.That(result.TotalPages, Is.EqualTo(2));
        }
    }
}
