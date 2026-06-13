using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Infrastructure.EntityConfigurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(t => t.Description).IsRequired().HasMaxLength(500);
        builder.Property(t => t.CreatedOn).IsRequired();
        builder.Property(t => t.ExternalId).HasMaxLength(100);
        builder.HasIndex(t => t.ExternalId).IsUnique().HasFilter("[ExternalId] IS NOT NULL");

        builder.HasOne(t => t.AccountNavigation)
               .WithMany(a => a.Transactions)
               .HasForeignKey(t => t.AccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CategoryNavigation)
               .WithMany(c => c.Transactions)
               .HasForeignKey(t => t.CategoryId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.TransferNavigation)
               .WithMany(tr => tr.Transactions)
               .HasForeignKey(t => t.TransferId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
