using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Infrastructure.EntityConfigurations;

public class CategoryBudgetConfiguration : IEntityTypeConfiguration<CategoryBudget>
{
    public void Configure(EntityTypeBuilder<CategoryBudget> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Month).HasColumnType("date");
        builder.Property(b => b.Amount).HasColumnType("decimal(18,2)");

        // Una sola previsione per categoria e mese.
        builder.HasIndex(b => new { b.CategoryId, b.Month }).IsUnique();

        // Eliminando una categoria (anche al reset demo/test) se ne eliminano le previsioni.
        builder.HasOne(b => b.CategoryNavigation)
               .WithMany(c => c.Budgets)
               .HasForeignKey(b => b.CategoryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
