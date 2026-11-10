using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Infrastructure.EntityConfigurations;

public class OneDriveConnectionConfiguration : IEntityTypeConfiguration<OneDriveConnection>
{
    public void Configure(EntityTypeBuilder<OneDriveConnection> builder)
    {
        builder.HasKey(c => c.Id);

        // Il device code è un token opaco di lunghezza variabile.
        builder.Property(c => c.DeviceCode).HasMaxLength(4000);
        builder.Property(c => c.UserCode).HasMaxLength(50);
        builder.Property(c => c.VerificationUri).HasMaxLength(500);
        builder.Property(c => c.DriveOwner).HasMaxLength(256);
        builder.Property(c => c.FolderId).HasMaxLength(200);
        builder.Property(c => c.FolderPath).HasMaxLength(400);
        builder.Property(c => c.ClientState).HasMaxLength(100);
        builder.Property(c => c.SubscriptionId).HasMaxLength(100);
        builder.Property(c => c.LastError).HasMaxLength(1000);

        // Un solo collegamento per utente.
        builder.HasIndex(c => c.UserId).IsUnique();
        // Le notifiche Graph identificano il collegamento con l'id della subscription.
        builder.HasIndex(c => c.SubscriptionId);

        builder.HasOne(c => c.UserNavigation)
               .WithMany()
               .HasForeignKey(c => c.UserId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);
    }
}
