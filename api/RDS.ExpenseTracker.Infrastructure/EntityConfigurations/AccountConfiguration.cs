using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Infrastructure.Utilities;

namespace RDS.ExpenseTracker.Infrastructure.EntityConfigurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(100);

        builder.HasMany(a => a.Transactions)
               .WithOne(t => t.AccountNavigation)
               .HasForeignKey(t => t.AccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new Account((int)AccountEnum.BBVA, AccountEnum.BBVA.GetDescription()),
            new Account((int)AccountEnum.Contanti, AccountEnum.Contanti.GetDescription()),
            new Account((int)AccountEnum.Hype, AccountEnum.Hype.GetDescription()),
            new Account((int)AccountEnum.PayPal, AccountEnum.PayPal.GetDescription()),
            new Account((int)AccountEnum.Satispay, AccountEnum.Satispay.GetDescription()),
            new Account((int)AccountEnum.Sella, AccountEnum.Sella.GetDescription()),
            new Account((int)AccountEnum.TradeRepublic, AccountEnum.TradeRepublic.GetDescription()),
            new Account((int)AccountEnum.TradeRepublicTrading, AccountEnum.TradeRepublicTrading.GetDescription())            
        );
    }
}