using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;

namespace RDS.ExpenseTracker.Domain.Services;

/// <summary>
/// Collegamento del OneDrive personale di un utente, fatto dall'app con il device code flow: l'utente apre la
/// pagina Microsoft, inserisce il codice e autorizza l'app; il frontend intanto chiede a intervalli l'esito.
/// Non disponibile per l'utente demo.
/// </summary>
public interface IOneDriveConnectionService : IService
{
    Task<Result<OneDriveConnectionDto>> GetAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Emette un nuovo codice di accesso (sostituisce un eventuale collegamento esistente solo al completamento).</summary>
    Task<Result<OneDriveConnectionDto>> StartAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Controlla se l'utente ha autorizzato l'app; in caso positivo crea la cartella con le sottocartelle delle
    /// banche, registra la baseline della delta query e la subscription Graph.
    /// </summary>
    Task<Result<OneDriveConnectionDto>> CompleteAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Elimina subscription, token e stato del collegamento. I file su OneDrive non vengono toccati.</summary>
    Task<Result<OneDriveConnectionDto>> DisconnectAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Verifica che l'utente possa chiedere una sincronizzazione manuale (collegamento attivo).</summary>
    Task<Result> EnsureCanSyncAsync(int userId, CancellationToken cancellationToken = default);
}
