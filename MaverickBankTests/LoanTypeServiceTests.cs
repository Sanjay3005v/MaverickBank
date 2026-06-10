using MaverickBank.DTOs.Loan;
using MaverickBank.Services.Loan;
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
    public class LoanTypeServiceTests
    {
        private MaverickBank.Data.AppDbContext _ctx = null!;
        private Mock<ILogger<LoanTypeService>> _logger = null!;
        private LoanTypeService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _ctx = TestHelpers.CreateDbContext();
            _logger = new Mock<ILogger<LoanTypeService>>();
            _sut = new LoanTypeService(_ctx, TestHelpers.CreateMapper(), _logger.Object);
        }

        [TearDown]
        public void TearDown() => _ctx.Dispose();


        [Test]
        public async Task CreateLoanTypeAsync_ValidData_ReturnsLoanType()
        {
            var dto = new CreateLoanTypeDto("Home Loan", 8.5m, 100_000m, 5_000_000m, 12, 240);
            var result = await _sut.CreateLoanTypeAsync(dto);

            Assert.That(result.LoanTypeId, Is.GreaterThan(0));
            Assert.That(result.LoanName, Is.EqualTo("Home Loan"));
            Assert.That(result.InterestRate, Is.EqualTo(8.5m));
        }

        [Test]
        public void CreateLoanTypeAsync_DuplicateName_ThrowsInvalidOperation()
        {
            TestHelpers.SeedLoanType(_ctx, "Personal Loan");
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.CreateLoanTypeAsync(new CreateLoanTypeDto("Personal Loan", 10m, 10_000m, 500_000m, 12, 60)));
        }

        [Test]
        public void CreateLoanTypeAsync_MinAmountGreaterThanMax_ThrowsInvalidOperation()
        {
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.CreateLoanTypeAsync(new CreateLoanTypeDto("Bad", 10m, 500_000m, 100_000m, 12, 60)));
        }

        [Test]
        public void CreateLoanTypeAsync_MinTenureGreaterThanMax_ThrowsInvalidOperation()
        {
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.CreateLoanTypeAsync(new CreateLoanTypeDto("Bad2", 10m, 10_000m, 500_000m, 60, 12)));
        }


        [Test]
        public async Task GetAllLoanTypesAsync_ReturnsPagedResult()
        {
            for (int i = 0; i < 12; i++)
                TestHelpers.SeedLoanType(_ctx, $"Loan Type {i}");

            var page1 = await _sut.GetAllLoanTypesAsync(1, 10);
            var page2 = await _sut.GetAllLoanTypesAsync(2, 10);

            Assert.That(page1.Data.Count(), Is.EqualTo(10));
            Assert.That(page2.Data.Count(), Is.EqualTo(2));
            Assert.That(page1.TotalCount, Is.EqualTo(12));
        }


        [Test]
        public async Task GetLoanTypeByIdAsync_Existing_ReturnsDto()
        {
            var lt = TestHelpers.SeedLoanType(_ctx, "Car Loan");
            var result = await _sut.GetLoanTypeByIdAsync(lt.LoanTypeId);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.LoanName, Is.EqualTo("Car Loan"));
        }

        [Test]
        public async Task GetLoanTypeByIdAsync_NotFound_ReturnsNull()
        {
            Assert.That(await _sut.GetLoanTypeByIdAsync(99999), Is.Null);
        }


        [Test]
        public async Task UpdateLoanTypeAsync_ValidData_UpdatesAndReturnsTrue()
        {
            var lt = TestHelpers.SeedLoanType(_ctx, "Education Loan");
            var dto = new CreateLoanTypeDto("Education Loan Updated", 9m, 20_000m, 1_000_000m, 12, 84);

            var result = await _sut.UpdateLoanTypeAsync(lt.LoanTypeId, dto);

            Assert.That(result, Is.True);
            Assert.That(_ctx.LoanTypes.Find(lt.LoanTypeId)!.LoanName, Is.EqualTo("Education Loan Updated"));
            Assert.That(_ctx.LoanTypes.Find(lt.LoanTypeId)!.InterestRate, Is.EqualTo(9m));
        }

        [Test]
        public async Task UpdateLoanTypeAsync_NotFound_ReturnsFalse()
        {
            var dto = new CreateLoanTypeDto("X", 10m, 10_000m, 500_000m, 12, 60);
            Assert.That(await _sut.UpdateLoanTypeAsync(99999, dto), Is.False);
        }

        [Test]
        public void UpdateLoanTypeAsync_DuplicateNameOnAnother_ThrowsInvalidOperation()
        {
            var lt1 = TestHelpers.SeedLoanType(_ctx, "Loan A");
            var lt2 = TestHelpers.SeedLoanType(_ctx, "Loan B");
            var dto = new CreateLoanTypeDto("Loan A", 10m, 10_000m, 500_000m, 12, 60);

            Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateLoanTypeAsync(lt2.LoanTypeId, dto));
        }


        [Test]
        public async Task DeleteLoanTypeAsync_Existing_DeletesAndReturnsTrue()
        {
            var lt = TestHelpers.SeedLoanType(_ctx, "Delete Me Loan");
            var result = await _sut.DeleteLoanTypeAsync(lt.LoanTypeId);

            Assert.That(result, Is.True);
            Assert.That(_ctx.LoanTypes.Find(lt.LoanTypeId), Is.Null);
        }

        [Test]
        public async Task DeleteLoanTypeAsync_NotFound_ReturnsFalse()
        {
            Assert.That(await _sut.DeleteLoanTypeAsync(99999), Is.False);
        }
    }
}
