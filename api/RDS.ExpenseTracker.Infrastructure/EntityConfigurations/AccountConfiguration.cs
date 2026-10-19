using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Infrastructure.EntityConfigurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Iban).HasMaxLength(34);

        // Un IBAN identifica un solo conto per utente, altrimenti il match dei trasferimenti sarebbe ambiguo.
        builder.HasIndex(a => new { a.UserId, a.Iban })
               .IsUnique()
               .HasFilter("[Iban] IS NOT NULL");

        builder.HasMany(a => a.Transactions)
               .WithOne(t => t.AccountNavigation)
               .HasForeignKey(t => t.AccountId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
