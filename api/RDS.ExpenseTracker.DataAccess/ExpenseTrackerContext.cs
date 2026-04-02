using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.DataAccess.Entities;
using RDS.ExpenseTracker.DataAccess.Seeds;
using RDS.ExpenseTracker.DataAccess.Utilities;

namespace RDS.ExpenseTracker.DataAccess
{
    public class ExpenseTrackerContext : DbContext
    {
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<FinancialAccount> FinancialAccounts { get; set; }
        public DbSet<Category> Categories { get; set; }

        #region Constructors
        public ExpenseTrackerContext()
        {

        }
        public ExpenseTrackerContext(DbContextOptions<ExpenseTrackerContext> opt) : base(opt)
        {

        }
        #endregion
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // for rapidly applying migrations; dev purposes only
            //optionsBuilder.UseSqlServer(AzureKeyVaultHandler.GetKeyVaultSecret(kvUri, secretName));

            optionsBuilder.UseSeeding((context, _) =>
            {
                if (!context.Set<Category>().AsNoTracking().Any())
                {
                    var seedCategories = SeedData.GetSeedCategories();
                    context.Set<Category>().AddRange(seedCategories);
                    context.SaveChanges();
                }
            });

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FinancialAccount>()
                .HasMany(x => x.Transactions)
                .WithOne(x => x.FinancialAccount)
                .HasForeignKey(x => x.FinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Category>()
                .HasMany(x => x.Transactions)
                .WithOne(x => x.Category)
                .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
