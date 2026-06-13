using System.ComponentModel;

namespace RDS.ExpenseTracker.Domain.Enums;

public enum AccountEnum
{
    [Description("Contanti")]
    Contanti = 1,

    [Description("Hype")]
    Hype = 2,

    [Description("Satispay")]
    Satispay = 3,

    [Description("Trade Republic")]
    TradeRepublic = 4,

    [Description("Sella")]
    Sella = 5,

    [Description("BBVA")]
    BBVA = 6,

    [Description("PayPal")]
    PayPal = 7,

    [Description("Trade Republic Trading")]
    TradeRepublicTrading = 8
}