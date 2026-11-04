namespace RDS.ExpenseTracker.Domain.Enums;

/// <summary>
/// Origine di un file di movimenti: determina quale servizio di import lo elabora.
/// </summary>
public enum BankFileSource
{
    Bbva,
    Sella,
    SatisPay,
    TradeRepublic,

    /// <summary>File Excel nel formato interno di ExpenseTracker (un foglio per conto).</summary>
    Excel,
}

/// <summary>
/// Formato fisico del file ricevuto.
/// </summary>
public enum BankFileFormat
{
    Csv,

    /// <summary>Cartella di lavoro Excel: per le banche viene convertita in CSV prima dell'import.</summary>
    Xlsx,
}
