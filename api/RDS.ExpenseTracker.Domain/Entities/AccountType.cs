using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Domain.Entities;

// Tipo di conto (banca o strumento) comune a tutti gli utenti. ImportSource indica quale import
// elabora gli estratti conto di quel tipo: null se il tipo non prevede import da file.
public class AccountType
{
    public const int BbvaId = 1;
    public const int SatispayId = 2;
    public const int TradeRepublicId = 3;
    public const int SellaId = 4;
    public const int RevolutId = 5;
    public const int ContantiId = 6;
    public const int HypeId = 7;
    public const int PayPalId = 8;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public BankFileSource? ImportSource { get; set; }
}
