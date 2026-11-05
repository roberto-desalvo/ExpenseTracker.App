using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Tests;

/// <summary>
/// Conti per i test degli import: un conto che si chiama come un tipo di conto (es. "Sella") riceve quel tipo,
/// come farebbe la migration sui dati esistenti; gli altri restano senza tipo.
/// </summary>
internal static class TestAccounts
{
    public static readonly IReadOnlyList<AccountType> Types =
    [
        new() { Id = AccountType.BbvaId, Name = "BBVA", ImportSource = BankFileSource.Bbva },
        new() { Id = AccountType.SatispayId, Name = "Satispay", ImportSource = BankFileSource.SatisPay },
        new() { Id = AccountType.TradeRepublicId, Name = "Trade Republic", ImportSource = BankFileSource.TradeRepublic },
        new() { Id = AccountType.SellaId, Name = "Sella", ImportSource = BankFileSource.Sella },
        new() { Id = AccountType.RevolutId, Name = "Revolut" },
        new() { Id = AccountType.ContantiId, Name = "Contanti" },
        new() { Id = AccountType.HypeId, Name = "Hype" },
        new() { Id = AccountType.PayPalId, Name = "PayPal" },
    ];

    public static Account Create(int id, string name, int userId)
    {
        var type = Types.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return new Account(id, name, userId)
        {
            AccountTypeId = type?.Id,
            AccountTypeNavigation = type,
        };
    }

    public static Account WithIban(this Account account, string iban)
    {
        account.Iban = iban;
        return account;
    }

    public static Account WithType(this Account account, int accountTypeId)
    {
        var type = Types.Single(t => t.Id == accountTypeId);
        account.AccountTypeId = type.Id;
        account.AccountTypeNavigation = type;
        return account;
    }
}
