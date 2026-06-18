using Microsoft.EntityFrameworkCore;
using MaverickBank.Models;

namespace MaverickBank.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<AccountType> AccountTypes => Set<AccountType>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<TransactionType> TransactionTypes => Set<TransactionType>();
        public DbSet<LoanType> LoanTypes => Set<LoanType>();
        public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
        public DbSet<Loan> Loans => Set<Loan>();
        public DbSet<LoanRepayment> LoanRepayments => Set<LoanRepayment>();
        public DbSet<AccountClosureRequest> AccountClosureRequests => Set<AccountClosureRequest>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
        public DbSet<AccountOpeningRequest> AccountOpeningRequests => Set<AccountOpeningRequest>();



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<User>()
                .HasOne<Role>()
                .WithMany()
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Account>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Account>()
                .HasOne<Branch>()
                .WithMany()
                .HasForeignKey(a => a.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Account>()
                .HasOne<AccountType>()
                .WithMany()
                .HasForeignKey(a => a.AccountTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Beneficiary>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne<TransactionType>()
                .WithMany()
                .HasForeignKey(t => t.TransactionTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction>()
                .HasOne<Account>()
                .WithMany()
                .HasForeignKey(t => t.FromAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Transaction>()
                .HasOne<Account>()
                .WithMany()
                .HasForeignKey(t => t.ToAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LoanApplication>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LoanApplication>()
                .HasOne<LoanType>()
                .WithMany()
                .HasForeignKey(l => l.LoanTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne<LoanApplication>()
                .WithMany()
                .HasForeignKey(l => l.LoanApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne<Account>()
                .WithMany()
                .HasForeignKey(l => l.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LoanRepayment>()
                .HasOne<Loan>()
                .WithMany()
                .HasForeignKey(r => r.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AccountClosureRequest>()
                .HasOne<Account>()
                .WithMany()
                .HasForeignKey(a => a.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PasswordResetOtp>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AccountOpeningRequest>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AccountOpeningRequest>()
                .HasOne<Branch>()
                .WithMany()
                .HasForeignKey(r => r.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AccountOpeningRequest>()
                .HasOne<AccountType>()
                .WithMany()
                .HasForeignKey(r => r.AccountTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
