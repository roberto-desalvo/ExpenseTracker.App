using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Infrastructure.EntityConfigurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.AzureOid).HasMaxLength(64);
        builder.Property(u => u.IsDemo).IsRequired().HasDefaultValue(false);

        // Filtrato: permette più utenti demo con AzureOid = NULL, ma garantisce
        // unicità per gli utenti reali provisionati via oid Azure AD.
        builder.HasIndex(u => u.AzureOid)
               .IsUnique()
               .HasFilter("[AzureOid] IS NOT NULL");

        builder.HasMany(u => u.Accounts)
               .WithOne(a => a.UserNavigation)
               .HasForeignKey(a => a.UserId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);
    }
}
