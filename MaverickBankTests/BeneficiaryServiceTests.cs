using MaverickBank.DTOs.Beneficiary;
using MaverickBank.Services.Beneficiary;
using Microsoft.Extensions.Logging;
using Moq;


namespace MaverickBankTests
{
    [TestFixture]
    public class BeneficiaryServiceTests
    {
        private MaverickBank.Data.AppDbContext _ctx = null!;
        private Mock<ILogger<BeneficiaryService>> _logger = null!;
        private BeneficiaryService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _ctx = TestHelpers.CreateDbContext();
            _logger = new Mock<ILogger<BeneficiaryService>>();
            _sut = new BeneficiaryService(_ctx, TestHelpers.CreateMapper(), _logger.Object);
        }

        [TearDown]
        public void TearDown() => _ctx.Dispose();


        [Test]
        public async Task AddBeneficiaryAsync_ValidData_ReturnsBeneficiary()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId);
            var dto = new AddBeneficiaryDto(user.UserId, "John Doe", "ACCT123456789012", "State Bank", "Main Branch", "SBIN0001234");

            var result = await _sut.AddBeneficiaryAsync(dto);

            Assert.That(result.BeneficiaryId, Is.GreaterThan(0));
            Assert.That(result.BeneficiaryName, Is.EqualTo("John Doe"));
            Assert.That(result.UserId, Is.EqualTo(user.UserId));
            Assert.That(result.IFSCCode, Is.EqualTo("SBIN0001234"));
        }

        [Test]
        public void AddBeneficiaryAsync_UserNotFound_ThrowsKeyNotFound()
        {
            var dto = new AddBeneficiaryDto(99999, "Ghost", "ACC123456789012", "Some Bank", "Branch", "SBIN0001234");

            Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.AddBeneficiaryAsync(dto));
        }


        [Test]
        public async Task GetBeneficiariesByUserIdAsync_ReturnsPagedResult()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId);

            for (int i = 0; i < 5; i++)
                _ctx.Beneficiaries.Add(new Models.Beneficiary
                {
                    UserId = user.UserId,
                    BeneficiaryName = $"Person{i}",
                    AccountNumber = $"ACCT{i:000000000000}",
                    BankName = "Bank",
                    BranchName = "Branch",
                    IFSCCode = "SBIN0001234",
                    CreatedAt = DateTime.UtcNow
                });
            _ctx.SaveChanges();

            var page1 = await _sut.GetBeneficiariesByUserIdAsync(user.UserId, 1, 3);
            var page2 = await _sut.GetBeneficiariesByUserIdAsync(user.UserId, 2, 3);

            Assert.That(page1.TotalCount, Is.EqualTo(5));
            Assert.That(page1.Data.Count(), Is.EqualTo(3));
            Assert.That(page2.Data.Count(), Is.EqualTo(2));
            Assert.That(page1.TotalPages, Is.EqualTo(2));
        }

        [Test]
        public async Task GetBeneficiariesByUserIdAsync_OtherUsersNotIncluded()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var u1 = TestHelpers.SeedUser(_ctx, role.RoleId, "b1@bank.com");
            var u2 = TestHelpers.SeedUser(_ctx, role.RoleId, "b2@bank.com");

            _ctx.Beneficiaries.Add(new Models.Beneficiary
            {
                UserId = u1.UserId,
                BeneficiaryName = "U1Ben",
                AccountNumber = "ACCT000000000001",
                BankName = "B",
                BranchName = "Br",
                IFSCCode = "SBIN0001234",
                CreatedAt = DateTime.UtcNow
            });
            _ctx.Beneficiaries.Add(new Models.Beneficiary
            {
                UserId = u2.UserId,
                BeneficiaryName = "U2Ben",
                AccountNumber = "ACCT000000000002",
                BankName = "B",
                BranchName = "Br",
                IFSCCode = "SBIN0001234",
                CreatedAt = DateTime.UtcNow
            });
            _ctx.SaveChanges();

            var result = await _sut.GetBeneficiariesByUserIdAsync(u1.UserId, 1, 10);

            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(result.Data.All(b => b.UserId == u1.UserId), Is.True);
        }


        [Test]
        public async Task DeleteBeneficiaryAsync_Existing_DeletesAndReturnsTrue()
        {
            var role = TestHelpers.SeedRole(_ctx);
            var user = TestHelpers.SeedUser(_ctx, role.RoleId);
            var b = new Models.Beneficiary
            {
                UserId = user.UserId,
                BeneficiaryName = "Del Me",
                AccountNumber = "ACCT000000000001",
                BankName = "B",
                BranchName = "Br",
                IFSCCode = "SBIN0001234",
                CreatedAt = DateTime.UtcNow
            };
            _ctx.Beneficiaries.Add(b);
            _ctx.SaveChanges();

            var result = await _sut.DeleteBeneficiaryAsync(b.BeneficiaryId);

            Assert.That(result, Is.True);
            Assert.That(_ctx.Beneficiaries.Find(b.BeneficiaryId), Is.Null);
        }

        [Test]
        public async Task DeleteBeneficiaryAsync_NotFound_ReturnsFalse()
        {
            Assert.That(await _sut.DeleteBeneficiaryAsync(99999), Is.False);
        }
    }
}
