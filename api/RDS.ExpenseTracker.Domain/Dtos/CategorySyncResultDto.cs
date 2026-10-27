namespace RDS.ExpenseTracker.Domain.Dtos;

public class CategorySyncResultDto
{
    // Movimenti nel range esclusi i trasferimenti.
    public int ExaminedTransactions { get; set; }

    // Movimenti la cui descrizione contiene un tag e che hanno cambiato categoria.
    public int UpdatedTransactions { get; set; }

    // Movimenti senza alcun tag corrispondente: lasciati con la categoria attuale.
    public int UnmatchedTransactions { get; set; }
}
