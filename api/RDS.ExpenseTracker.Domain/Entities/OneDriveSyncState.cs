namespace RDS.ExpenseTracker.Domain.Entities;

// Stato (riga singola, Id = 1) dell'import automatico da OneDrive: posizione della delta query,
// subscription Graph attiva e lock che impedisce due sincronizzazioni contemporanee.
public class OneDriveSyncState
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    // Link restituito dall'ultima delta query; null = baseline non ancora fatta.
    public string? DeltaLink { get; set; }

    public string? SubscriptionId { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }

    // Istante (UTC) fino a cui una sincronizzazione in corso detiene il lock; scade da solo se il processo muore.
    public DateTime? LockedUntil { get; set; }

    public DateTime? LastSyncOn { get; set; }
}
