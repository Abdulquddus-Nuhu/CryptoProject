using CryptoProject.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CryptoProject.Entities;
using Microsoft.EntityFrameworkCore.Design;
using System.Data;

namespace CryptoProject.Data
{
    public class AppDbContext : IdentityDbContext<User, Role, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<USDAccount> USDAccounts => Set<USDAccount>();
        public DbSet<LedgerAccount> LedgerAccounts => Set<LedgerAccount>();
        public DbSet<Transaction> Transactions => Set<Transaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the one-to-one relationship between User and Wallet
            modelBuilder.Entity<User>()
                .HasOne(u => u.Wallet)  // User has one Wallet
                .WithOne(w => w.User)  // Wallet is associated with one User
                .HasForeignKey<Wallet>(w => w.UserId);  // UserId is the foreign key in Wallet

            modelBuilder.Entity<User>()
               .HasOne(u => u.LedgerAccount)  // User has one Wallet
               .WithOne(w => w.User)  // Wallet is associated with one User
               .HasForeignKey<LedgerAccount>(w => w.UserId);
            
            modelBuilder.Entity<User>()
               .HasOne(u => u.USDAccount)  // User has one Wallet
               .WithOne(w => w.User)  // Wallet is associated with one User
               .HasForeignKey<USDAccount>(w => w.UserId);
        }


        public async Task<bool> TrySaveChangesAsync()
        {
            try
            {
                await SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

    }

    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            string connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION") ?? string.Empty;
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
