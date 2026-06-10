using MaverickBank.DTOs.Branch;
using MaverickBank.Services.Branch;
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
    public class BranchServiceTests
    {
        private MaverickBank.Data.AppDbContext _ctx = null!;
        private Mock<ILogger<BranchService>> _logger = null!;
        private BranchService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _ctx = TestHelpers.CreateDbContext();
            _logger = new Mock<ILogger<BranchService>>();
            _sut = new BranchService(_ctx, TestHelpers.CreateMapper(), _logger.Object);
        }

        [TearDown]
        public void TearDown() => _ctx.Dispose();


        [Test]
        public async Task CreateBranchAsync_ValidData_ReturnsBranch()
        {
            var dto = new CreateBranchDto("North Branch", "MVRK0001111", "1 North St", "Delhi", "Delhi", "110001", "011-11111111");
            var result = await _sut.CreateBranchAsync(dto);

            Assert.That(result.BranchId, Is.GreaterThan(0));
            Assert.That(result.IFSCCode, Is.EqualTo("MVRK0001111"));
            Assert.That(result.BranchName, Is.EqualTo("North Branch"));
        }

        [Test]
        public void CreateBranchAsync_DuplicateIFSC_ThrowsInvalidOperation()
        {
            TestHelpers.SeedBranch(_ctx, "MVRK0001234");
            var dto = new CreateBranchDto("Dup", "MVRK0001234", "X", "Y", "Z", "123456", "0");

            Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateBranchAsync(dto));
        }

        [Test]
        public async Task GetBranchByIdAsync_Existing_ReturnsBranch()
        {
            var branch = TestHelpers.SeedBranch(_ctx, "MVRK0002222");
            var result = await _sut.GetBranchByIdAsync(branch.BranchId);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.BranchId, Is.EqualTo(branch.BranchId));
        }

        [Test]
        public async Task GetBranchByIdAsync_NotFound_ReturnsNull()
        {
            Assert.That(await _sut.GetBranchByIdAsync(99999), Is.Null);
        }


        [Test]
        public async Task GetAllBranchesAsync_ReturnsPagedResult()
        {
            for (int i = 0; i < 12; i++)
                TestHelpers.SeedBranch(_ctx, $"MVRK{i:0000000}");

            var page1 = await _sut.GetAllBranchesAsync(1, 10);
            var page2 = await _sut.GetAllBranchesAsync(2, 10);

            Assert.That(page1.Data.Count(), Is.EqualTo(10));
            Assert.That(page2.Data.Count(), Is.EqualTo(2));
            Assert.That(page1.TotalPages, Is.EqualTo(2));
        }


        [Test]
        public async Task UpdateBranchAsync_ValidData_UpdatesBranch()
        {
            var branch = TestHelpers.SeedBranch(_ctx, "MVRK0003333");
            var dto = new CreateBranchDto("Updated Name", "MVRK0003333", "2 New St", "Mumbai", "Maharashtra", "400001", "022-99999999");

            var result = await _sut.UpdateBranchAsync(branch.BranchId, dto);

            Assert.That(result, Is.True);
            Assert.That(_ctx.Branches.Find(branch.BranchId)!.BranchName, Is.EqualTo("Updated Name"));
        }

        [Test]
        public async Task UpdateBranchAsync_NotFound_ReturnsFalse()
        {
            var dto = new CreateBranchDto("X", "MVRK0004444", "X", "X", "X", "123456", "0");
            Assert.That(await _sut.UpdateBranchAsync(99999, dto), Is.False);
        }

        [Test]
        public void UpdateBranchAsync_DuplicateIFSCOnAnotherBranch_ThrowsInvalidOperation()
        {
            var b1 = TestHelpers.SeedBranch(_ctx, "MVRK0005555");
            var b2 = TestHelpers.SeedBranch(_ctx, "MVRK0006666");
            var dto = new CreateBranchDto("B2 Updated", "MVRK0005555", "X", "X", "X", "123456", "0");

            Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateBranchAsync(b2.BranchId, dto));
        }


        [Test]
        public async Task DeleteBranchAsync_Existing_DeletesAndReturnsTrue()
        {
            var branch = TestHelpers.SeedBranch(_ctx, "MVRK0007777");
            var result = await _sut.DeleteBranchAsync(branch.BranchId);

            Assert.That(result, Is.True);
            Assert.That(_ctx.Branches.Find(branch.BranchId), Is.Null);
        }

        [Test]
        public async Task DeleteBranchAsync_NotFound_ReturnsFalse()
        {
            Assert.That(await _sut.DeleteBranchAsync(99999), Is.False);
        }


        [Test]
        public async Task SearchBranchesByNameAsync_MatchingName_ReturnsBranches()
        {
            var branch = TestHelpers.SeedBranch(_ctx, "MVRK0008888");
            branch.BranchName = "Chennai Central";
            _ctx.SaveChanges();

            var result = await _sut.SearchBranchesByNameAsync("Chennai");

            Assert.That(result.Any(), Is.True);
            Assert.That(result.All(b => b.BranchName.Contains("Chennai")), Is.True);
        }

        [Test]
        public async Task SearchBranchesByNameAsync_NoMatch_ReturnsEmpty()
        {
            TestHelpers.SeedBranch(_ctx, "MVRK0009999");

            var result = await _sut.SearchBranchesByNameAsync("ZZZNoMatch");

            Assert.That(result.Any(), Is.False);
        }
    }
}
