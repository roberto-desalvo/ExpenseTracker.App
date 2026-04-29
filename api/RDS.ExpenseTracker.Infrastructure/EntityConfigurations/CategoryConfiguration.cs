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

        builder.HasData(Category.CreateSeedCategories());

        builder.HasMany(c => c.Transactions)
               .WithOne(t => t.CategoryNavigation)
               .HasForeignKey(t => t.CategoryId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
