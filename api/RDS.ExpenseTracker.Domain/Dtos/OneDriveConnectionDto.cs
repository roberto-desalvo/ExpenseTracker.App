using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Domain.Dtos;

// Stato del collegamento OneDrive dell'utente, esposto al frontend (Impostazioni → Import).
public class OneDriveConnectionDto
{
    // False se l'import da OneDrive non è configurato in questo ambiente.
    public bool Available { get; set; }

    public OneDriveConnectionStatus Status { get; set; }

    // Cartella dell'app nel drive (nota dopo il collegamento, es. Apps/ExpenseTracker) e sottocartelle
    // delle banche in cui salvare gli estratti conto.
    public string? FolderPath { get; set; }
    public IReadOnlyList<string> BankFolders { get; set; } = [];

    // Solo con Status = Pending: codice da inserire sulla pagina Microsoft e intervallo tra i controlli.
    public string? UserCode { get; set; }
    public string? VerificationUri { get; set; }
    public DateTime? CodeExpiresOn { get; set; }
    public int? PollIntervalSeconds { get; set; }

    public string? DriveOwner { get; set; }
    public DateTime? ConnectedOn { get; set; }
    public DateTime? LastSyncOn { get; set; }

    // True se le notifiche Graph sono attive: i file vengono importati da soli poco dopo il caricamento.
    public bool AutomaticSync { get; set; }

    public string? LastError { get; set; }
}
