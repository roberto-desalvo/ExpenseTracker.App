using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Infrastructure;

public class ExpenseTrackerContext : DbContext
{
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Transfer> Transfers { get; set; }

    public ExpenseTrackerContext(DbContextOptions<ExpenseTrackerContext> opt) : base(opt)
    {
    }        

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExpenseTrackerContext).Assembly);
    }
}
