using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Infrastructure.EntityConfigurations;

public class ImportLogConfiguration : IEntityTypeConfiguration<ImportLog>
{
    public void Configure(EntityTypeBuilder<ImportLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ImportedOn).IsRequired();
        builder.Property(l => l.Source).IsRequired().HasMaxLength(50);
        builder.Property(l => l.FileName).HasMaxLength(260);
        builder.Property(l => l.ErrorMessage).HasMaxLength(4000);

        builder.HasIndex(l => new { l.UserId, l.ImportedOn });

        builder.HasOne(l => l.UserNavigation)
               .WithMany()
               .HasForeignKey(l => l.UserId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);

        // Il log sopravvive all'eliminazione del conto (es. reset dei dati demo/test).
        builder.HasOne(l => l.AccountNavigation)
               .WithMany()
               .HasForeignKey(l => l.AccountId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
