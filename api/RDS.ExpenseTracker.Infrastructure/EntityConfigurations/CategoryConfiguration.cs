using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Infrastructure.EntityConfigurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Tags).HasMaxLength(500);

        // Nessun seed globale: le categorie di default vengono create per ogni utente
        // (alla creazione dell'utente e al reset dei dati demo/test).
        builder.HasOne(c => c.UserNavigation)
               .WithMany(u => u.Categories)
               .HasForeignKey(c => c.UserId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);

        // Al più una categoria per tipo di sistema per utente, così la ricerca per Kind è univoca.
        builder.HasIndex(c => new { c.UserId, c.Kind })
               .IsUnique()
               .HasFilter("[Kind] IS NOT NULL");

        builder.HasMany(c => c.Transactions)
               .WithOne(t => t.CategoryNavigation)
               .HasForeignKey(t => t.CategoryId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
