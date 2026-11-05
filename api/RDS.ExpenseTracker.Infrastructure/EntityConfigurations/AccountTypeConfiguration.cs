using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Infrastructure.EntityConfigurations;

public class AccountTypeConfiguration : IEntityTypeConfiguration<AccountType>
{
    public void Configure(EntityTypeBuilder<AccountType> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.Name).IsUnique();

        // Catalogo fisso creato dalla migration: gli id sono referenziati dal codice (AccountType.*Id).
        builder.HasData(
            new AccountType { Id = AccountType.BbvaId, Name = "BBVA", ImportSource = BankFileSource.Bbva },
            new AccountType { Id = AccountType.SatispayId, Name = "Satispay", ImportSource = BankFileSource.SatisPay },
            new AccountType { Id = AccountType.TradeRepublicId, Name = "Trade Republic", ImportSource = BankFileSource.TradeRepublic },
            new AccountType { Id = AccountType.SellaId, Name = "Sella", ImportSource = BankFileSource.Sella },
            new AccountType { Id = AccountType.RevolutId, Name = "Revolut" },
            new AccountType { Id = AccountType.ContantiId, Name = "Contanti" },
            new AccountType { Id = AccountType.HypeId, Name = "Hype" },
            new AccountType { Id = AccountType.PayPalId, Name = "PayPal" });
    }
}
