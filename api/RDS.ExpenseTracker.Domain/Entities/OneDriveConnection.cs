using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Domain.Entities;

// Collegamento del OneDrive personale di un utente (al massimo uno per utente) per l'import automatico:
// autorizzazione in corso (device code), cartella osservata, posizione della delta query, subscription Graph
// e lock che impedisce due sincronizzazioni contemporanee. Il refresh token non è qui ma in Key Vault.
public class OneDriveConnection
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? UserNavigation { get; set; }

    public OneDriveConnectionStatus Status { get; set; }

    // Autorizzazione in corso (Status = Pending): codice da inserire sul sito Microsoft e sua scadenza.
    public string? DeviceCode { get; set; }
    public string? UserCode { get; set; }
    public string? VerificationUri { get; set; }
    public DateTime? DeviceCodeExpiresAt { get; set; }
    public int? PollIntervalSeconds { get; set; }

    // Proprietario del drive collegato (nome mostrato all'utente) e cartella osservata.
    public string? DriveOwner { get; set; }
    public string? FolderId { get; set; }

    // Segreto inviato da Graph in ogni notifica della subscription di questo collegamento.
    public string? ClientState { get; set; }

    // Link restituito dall'ultima delta query; null = baseline non ancora fatta.
    public string? DeltaLink { get; set; }

    public string? SubscriptionId { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }

    // Istante (UTC) fino a cui una sincronizzazione in corso detiene il lock; scade da solo se il processo muore.
    public DateTime? LockedUntil { get; set; }

    public DateTime? ConnectedOn { get; set; }
    public DateTime? LastSyncOn { get; set; }

    // Ultimo problema del collegamento (autorizzazione, subscription), mostrato all'utente.
    public string? LastError { get; set; }
}
