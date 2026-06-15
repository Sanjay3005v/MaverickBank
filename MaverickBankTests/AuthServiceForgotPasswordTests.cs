using MaverickBank.DTOs.Auth;
using MaverickBank.Services.Auth;
using MaverickBank.Services.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;

namespace MaverickBankTests
{
    [TestFixture]
    public class AuthServiceForgotPasswordTests
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
        public async Task ForgotPasswordAsync_ExistingEmail_SendsOtpEmail()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, "forgot@bank.com");

            await _sut.ForgotPasswordAsync(new ForgotPasswordDto("forgot@bank.com"));

            Assert.That(_ctx.PasswordResetOtps.Any(o => o.UserId == user.UserId), Is.True);
            _email.Verify(e => e.SendEmailAsync("forgot@bank.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task ForgotPasswordAsync_UnknownEmail_DoesNotThrowOrSendEmail()
        {
            await _sut.ForgotPasswordAsync(new ForgotPasswordDto("nobody@bank.com"));

            _email.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ForgotPasswordAsync_GeneratesSixDigitOtp()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, "otp@bank.com");

            await _sut.ForgotPasswordAsync(new ForgotPasswordDto("otp@bank.com"));

            var entry = _ctx.PasswordResetOtps.Single(o => o.UserId == user.UserId);
            Assert.That(entry.Otp, Has.Length.EqualTo(6));
            Assert.That(int.TryParse(entry.Otp, out _), Is.True);
            Assert.That(entry.IsUsed, Is.False);
        }


        [Test]
        public async Task ResetPasswordAsync_ValidOtp_UpdatesPasswordAndMarksUsed()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, "reset@bank.com");
            var entry = TestHelpers.SeedPasswordResetOtp(_ctx, user.UserId, "123456", DateTime.UtcNow.AddMinutes(10));

            await _sut.ResetPasswordAsync(new ResetPasswordDto("reset@bank.com", "123456", "NewPass@123"));

            var updatedUser = _ctx.Users.Find(user.UserId)!;
            Assert.That(BCrypt.Net.BCrypt.Verify("NewPass@123", updatedUser.PasswordHash), Is.True);

            var updatedEntry = _ctx.PasswordResetOtps.Find(entry.Id)!;
            Assert.That(updatedEntry.IsUsed, Is.True);
        }

        [Test]
        public void ResetPasswordAsync_ExpiredOtp_ThrowsInvalidOperation()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, "expired@bank.com");
            TestHelpers.SeedPasswordResetOtp(_ctx, user.UserId, "654321", DateTime.UtcNow.AddMinutes(-5));

            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ResetPasswordAsync(new ResetPasswordDto("expired@bank.com", "654321", "NewPass@123")));
        }

        [Test]
        public void ResetPasswordAsync_WrongOtp_ThrowsInvalidOperation()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, "wrongotp@bank.com");
            TestHelpers.SeedPasswordResetOtp(_ctx, user.UserId, "111111", DateTime.UtcNow.AddMinutes(10));

            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ResetPasswordAsync(new ResetPasswordDto("wrongotp@bank.com", "999999", "NewPass@123")));
        }

        [Test]
        public void ResetPasswordAsync_AlreadyUsedOtp_ThrowsInvalidOperation()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId, "used@bank.com");
            TestHelpers.SeedPasswordResetOtp(_ctx, user.UserId, "222222", DateTime.UtcNow.AddMinutes(10), isUsed: true);

            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ResetPasswordAsync(new ResetPasswordDto("used@bank.com", "222222", "NewPass@123")));
        }

        [Test]
        public void ResetPasswordAsync_UnknownEmail_ThrowsInvalidOperation()
        {
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ResetPasswordAsync(new ResetPasswordDto("ghost@bank.com", "123456", "NewPass@123")));
        }
    }
}