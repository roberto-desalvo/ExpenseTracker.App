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
        // indice dedicato alla FK: quello composito sotto è filtrato e non copre i movimenti senza ExternalId
        builder.HasIndex(t => t.AccountId);
        // univoco per conto (quindi per utente): lo stesso movimento può essere importato da utenti diversi
        builder.HasIndex(t => new { t.AccountId, t.ExternalId }).IsUnique().HasFilter("[ExternalId] IS NOT NULL");

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
