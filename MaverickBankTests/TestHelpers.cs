using AutoMapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using MaverickBank.Data;
using MaverickBank.Models;
using Microsoft.EntityFrameworkCore;
using MaverickBank.MappingProfile;

namespace MaverickBankTests
{
    internal class TestHelpers
    {
        public static AppDbContext CreateDbContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        public static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>());
            return config.CreateMapper();
        }


        public static Role SeedRole(AppDbContext ctx, string roleName = "Customer")
        {
            var role = new Role { RoleName = roleName };
            ctx.Roles.Add(role);
            ctx.SaveChanges();
            return role;
        }

        public static User SeedUser(AppDbContext ctx, int roleId, string email = "test@bank.com", bool isActive = true)
        {
            var user = new User
            {
                RoleId = roleId,
                FirstName = "Test",
                LastName = "User",
                Email = email,
                PhoneNumber = $"9{Random.Shared.Next(100000000, 999999999)}",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@1"),
                Gender = "Male",
                DateOfBirth = new DateTime(1990, 1, 1),
                AadhaarNumber = $"{Random.Shared.NextInt64(100000000000L, 999999999999L)}",
                PANNumber = $"ABCDE{Random.Shared.Next(1000, 9999)}F",
                AddressLine1 = "123 Main St",
                City = "Chennai",
                State = "Tamil Nadu",
                Pincode = "600001",
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            ctx.Users.Add(user);
            ctx.SaveChanges();
            return user;
        }

        public static Branch SeedBranch(AppDbContext ctx, string ifsc = "MVRK0001234")
        {
            var branch = new Branch
            {
                BranchName = "Main Branch",
                IFSCCode = ifsc,
                AddressLine1 = "10 Bank Road",
                City = "Chennai",
                State = "Tamil Nadu",
                Pincode = "600001",
                PhoneNumber = "044-12345678",
                CreatedAt = DateTime.UtcNow
            };
            ctx.Branches.Add(branch);
            ctx.SaveChanges();
            return branch;
        }

        public static AccountType SeedAccountType(AppDbContext ctx, string typeName = "Savings")
        {
            var at = new AccountType { TypeName = typeName };
            ctx.AccountTypes.Add(at);
            ctx.SaveChanges();
            return at;
        }

        public static Account SeedAccount(AppDbContext ctx, int userId, int branchId, int accountTypeId,
            decimal balance = 10_000m, string status = "Active")
        {
            var account = new Account
            {
                UserId = userId,
                BranchId = branchId,
                AccountTypeId = accountTypeId,
                AccountNumber = $"ACCT{Random.Shared.Next(100000000, 999999999)}",
                Balance = balance,
                Status = status,
                OpenedDate = DateTime.UtcNow
            };
            ctx.Accounts.Add(account);
            ctx.SaveChanges();
            return account;
        }

        public static TransactionType SeedTransactionType(AppDbContext ctx, string name)
        {
            var tt = new TransactionType { TypeName = name };
            ctx.TransactionTypes.Add(tt);
            ctx.SaveChanges();
            return tt;
        }

        public static LoanType SeedLoanType(AppDbContext ctx,
            string name = "Personal Loan",
            decimal rate = 10m,
            decimal min = 10_000m,
            decimal max = 500_000m,
            int minTenure = 12,
            int maxTenure = 60)
        {
            var lt = new LoanType
            {
                LoanName = name,
                InterestRate = rate,
                MinimumAmount = min,
                MaximumAmount = max,
                MinimumTenureMonths = minTenure,
                MaximumTenureMonths = maxTenure
            };
            ctx.LoanTypes.Add(lt);
            ctx.SaveChanges();
            return lt;
        }

        public static LoanApplication SeedLoanApplication(AppDbContext ctx, int userId, int loanTypeId,
            string status = "Pending", decimal amount = 50_000m)
        {
            var app = new LoanApplication
            {
                UserId = userId,
                LoanTypeId = loanTypeId,
                RequestedAmount = amount,
                TenureMonths = 24,
                Purpose = "Home renovation",
                MonthlyIncome = 60_000m,
                ApplicationStatus = status,
                AppliedDate = DateTime.UtcNow
            };
            ctx.LoanApplications.Add(app);
            ctx.SaveChanges();
            return app;
        }

        public static Loan SeedLoan(AppDbContext ctx, long appId, long accountId,
            decimal approvedAmount = 50_000m, string status = "Active")
        {
            var loan = new Loan
            {
                LoanApplicationId = appId,
                AccountId = accountId,
                ApprovedAmount = approvedAmount,
                InterestRate = 10m,
                TenureMonths = 24,
                EMIAmount = approvedAmount / 24,
                OutstandingAmount = approvedAmount,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(24),
                LoanStatus = status
            };
            ctx.Loans.Add(loan);
            ctx.SaveChanges();
            return loan;
        }
        public static MaverickBank.Models.PasswordResetOtp SeedPasswordResetOtp(AppDbContext ctx, int userId, string otp, DateTime expiryDate, bool isUsed = false)
        {
            var entry = new MaverickBank.Models.PasswordResetOtp
            {
                UserId = userId,
                Otp = otp,
                ExpiryDate = expiryDate,
                IsUsed = isUsed,
                CreatedAt = DateTime.UtcNow
            };
            ctx.PasswordResetOtps.Add(entry);
            ctx.SaveChanges();
            return entry;
        }
    }
}
