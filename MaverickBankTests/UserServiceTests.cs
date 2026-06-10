using MaverickBank.DTOs.User;
using MaverickBank.Services.AuditLog;
using MaverickBank.Services.User;
using Microsoft.Extensions.Logging;
using Moq;


namespace MaverickBankTests
{
    [TestFixture]
    public class UserServiceTests
    {
        private MaverickBank.Data.AppDbContext _ctx = null!;
        private Mock<IAuditLogService> _audit = null!;
        private Mock<ILogger<UserService>> _logger = null!;
        private UserService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _ctx = TestHelpers.CreateDbContext();
            _audit = new Mock<IAuditLogService>();
            _logger = new Mock<ILogger<UserService>>();
            _sut = new UserService(_ctx, TestHelpers.CreateMapper(), _audit.Object, _logger.Object);
        }

        [TearDown]
        public void TearDown() => _ctx.Dispose();

        [Test]
        public async Task RegisterAsync_ValidData_CreatesInactiveUser()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var result = await _sut.RegisterAsync(Build("new@bank.com", "9876543210", "123456789012", "ABCDE1234F", role.RoleId));

            Assert.That(result.Email, Is.EqualTo("new@bank.com"));
            Assert.That(result.IsActive, Is.False);
            Assert.That(result.RoleId, Is.EqualTo(role.RoleId));
        }

        [Test]
        public void RegisterAsync_DuplicateEmail_ThrowsInvalidOperation()
        {
            var role = TestHelpers.SeedRole(_ctx);
            TestHelpers.SeedUser(_ctx, role.RoleId, "dup@bank.com");

            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.RegisterAsync(Build("dup@bank.com", "9000000001", "100000000001", "BBCDE1234F", role.RoleId)));
        }

        [Test]
        public void RegisterAsync_DuplicatePhone_ThrowsInvalidOperation()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, "a@bank.com");

            Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.RegisterAsync(Build("b@bank.com", user.PhoneNumber, "200000000001", "CBCDE1234F", role.RoleId)));
        }

        [Test]
        public void RegisterAsync_InvalidRole_ThrowsKeyNotFound()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.RegisterAsync(Build("orphan@bank.com", "9111111111", "300000000001", "DBCDE1234F", 999)));
        }

        [Test]
        public async Task RegisterAsync_PasswordIsStoredHashed()
        {
            var role = TestHelpers.SeedRole(_ctx);
            await _sut.RegisterAsync(Build("hash@bank.com", "9222222222", "400000000001", "EBCDE1234F", role.RoleId));

            var dbUser = _ctx.Users.First(u => u.Email == "hash@bank.com");
            Assert.That(BCrypt.Net.BCrypt.Verify("Password@1", dbUser.PasswordHash), Is.True);
        }

        [Test]
        public async Task GetUserByIdAsync_ExistingId_ReturnsUser()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, "get@bank.com");
            var result = await _sut.GetUserByIdAsync(user.UserId);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Email, Is.EqualTo("get@bank.com"));
        }

        [Test]
        public async Task GetUserByIdAsync_NonExistingId_ReturnsNull()
        {
            Assert.That(await _sut.GetUserByIdAsync(99999), Is.Null);
        }

        [Test]
        public async Task GetAllUsersAsync_ReturnsCorrectPage()
        {
            var role = TestHelpers.SeedRole(_ctx);
            for (int i = 0; i < 15; i++)
                TestHelpers.SeedUser(_ctx, role.RoleId, $"u{i}@bank.com");

            var page1 = await _sut.GetAllUsersAsync(1, 10);
            var page2 = await _sut.GetAllUsersAsync(2, 10);

            Assert.That(page1.Data.Count(), Is.EqualTo(10));
            Assert.That(page2.Data.Count(), Is.EqualTo(5));
            Assert.That(page1.TotalCount, Is.EqualTo(15));
            Assert.That(page1.TotalPages, Is.EqualTo(2));
        }

        [Test]
        public async Task UpdateUserAsync_ValidData_UpdatesUser()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, "upd@bank.com");
            var dto = new UpdateUserDto("NewFirst", "NewLast", user.PhoneNumber,
                                          "456 New St", null, "Mumbai", "Maharashtra", "400001");

            var result = await _sut.UpdateUserAsync(user.UserId, dto);

            Assert.That(result, Is.True);
            Assert.That(_ctx.Users.Find(user.UserId)!.FirstName, Is.EqualTo("NewFirst"));
        }

        [Test]
        public async Task UpdateUserAsync_NonExistingId_ReturnsFalse()
        {
            var dto = new UpdateUserDto("A", null, "9000000000", "B", null, "C", "D", "123456");
            Assert.That(await _sut.UpdateUserAsync(99999, dto), Is.False);
        }

        [Test]
        public async Task SetUserActiveStatusAsync_ActivatesUser()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, "act@bank.com", isActive: false);

            Assert.That(await _sut.SetUserActiveStatusAsync(user.UserId, true), Is.True);
            Assert.That(_ctx.Users.Find(user.UserId)!.IsActive, Is.True);
        }

        [Test]
        public async Task SetUserActiveStatusAsync_DeactivatesUser()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, "deact@bank.com", isActive: true);

            await _sut.SetUserActiveStatusAsync(user.UserId, false);
            Assert.That(_ctx.Users.Find(user.UserId)!.IsActive, Is.False);
        }

        [Test]
        public async Task SetUserActiveStatusAsync_NonExistingId_ReturnsFalse()
        {
            Assert.That(await _sut.SetUserActiveStatusAsync(99999, true), Is.False);
        }

        private static CreateUserDto Build(string email, string phone, string aadhaar, string pan, int roleId) =>
            new(roleId, "First", "Last", email, phone, "Password@1",
                "Male", new DateTime(1990, 1, 1), aadhaar, pan,
                "123 St", null, "Chennai", "Tamil Nadu", "600001");
    }
}
