using MaverickBank.DTOs.Auth;
using MaverickBank.Services.Auth;
using MaverickBank.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;


namespace MaverickBankTests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private MaverickBank.Data.AppDbContext _ctx = null!;
        private Mock<IJwtTokenService> _jwt = null!;
        private Mock<IEmailService> _email = null!;
        private Mock<IConfiguration> _config = null!;
        private Mock<ILogger<AuthService>> _logger = null!;
        private AuthService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _ctx = TestHelpers.CreateDbContext();
            _jwt = new Mock<IJwtTokenService>();
            _email = new Mock<IEmailService>();
            _config = new Mock<IConfiguration>();
            _logger = new Mock<ILogger<AuthService>>();

            _config.Setup(c => c["JwtSettings:ExpiryMinutes"]).Returns("60");

            _sut = new AuthService(_ctx, _jwt.Object, _config.Object, _logger.Object, _email.Object);
        }

        [TearDown]
        public void TearDown() => _ctx.Dispose();

        [Test]
        public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
        {
            var role = TestHelpers.SeedRole(_ctx, "Customer");
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, "login@bank.com", isActive: true);
            _jwt.Setup(j => j.GenerateToken(It.IsAny<MaverickBank.Models.User>(), "Customer")).Returns("mock.jwt.token");

            var result = await _sut.LoginAsync(new LoginRequestDto("login@bank.com", "Password@1"));

            Assert.That(result.Token, Is.EqualTo("mock.jwt.token"));
            Assert.That(result.Email, Is.EqualTo("login@bank.com"));
            Assert.That(result.Role, Is.EqualTo("Customer"));
            Assert.That(result.UserId, Is.EqualTo(user.UserId));
        }

        [Test]
        public void LoginAsync_WrongPassword_ThrowsUnauthorized()
        {
            var role = TestHelpers.SeedRole(_ctx);
            TestHelpers.SeedUser(_ctx, role.RoleId, "pw@bank.com");

            Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _sut.LoginAsync(new LoginRequestDto("pw@bank.com", "WrongPass")));
        }

        [Test]
        public void LoginAsync_UnknownEmail_ThrowsUnauthorized()
        {
            Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _sut.LoginAsync(new LoginRequestDto("nobody@bank.com", "Password@1")));
        }

        [Test]
        public void LoginAsync_InactiveUser_ThrowsUnauthorized()
        {
            var role = TestHelpers.SeedRole(_ctx);
            TestHelpers.SeedUser(_ctx, role.RoleId, "inactive@bank.com", isActive: false);

            Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _sut.LoginAsync(new LoginRequestDto("inactive@bank.com", "Password@1")));
        }

        [Test]
        public async Task LoginAsync_ReturnsExpiryApproximately60Minutes()
        {
            var role = TestHelpers.SeedRole(_ctx);
            TestHelpers.SeedUser(_ctx, role.RoleId, "exp@bank.com");
            _jwt.Setup(j => j.GenerateToken(It.IsAny<MaverickBank.Models.User>(), It.IsAny<string>())).Returns("tok");

            var result = await _sut.LoginAsync(new LoginRequestDto("exp@bank.com", "Password@1"));

            Assert.That(result.ExpiresAt,
                Is.InRange(DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddMinutes(61)));
        }
    }
}
