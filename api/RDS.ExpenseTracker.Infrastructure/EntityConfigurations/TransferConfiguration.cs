using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Infrastructure.EntityConfigurations;

public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.CreatedOn).IsRequired();
        builder.Property(t => t.ExternalId).HasMaxLength(100);
        builder.HasIndex(t => t.ExternalId).IsUnique().HasFilter("[ExternalId] IS NOT NULL");

        builder.HasMany(t => t.Transactions)
               .WithOne(tr => tr.TransferNavigation)
               .HasForeignKey(tr => tr.TransferId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
