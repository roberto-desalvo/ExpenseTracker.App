using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Infrastructure.EntityConfigurations;

public class OneDriveSyncStateConfiguration : IEntityTypeConfiguration<OneDriveSyncState>
{
    public void Configure(EntityTypeBuilder<OneDriveSyncState> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        // I deltaLink di Graph contengono un token opaco di lunghezza variabile.
        builder.Property(s => s.DeltaLink).HasMaxLength(4000);
        builder.Property(s => s.SubscriptionId).HasMaxLength(100);

        // Riga singola creata dalla migration: il lock è un UPDATE condizionato su questa riga.
        builder.HasData(new OneDriveSyncState { Id = OneDriveSyncState.SingletonId });
    }
}
