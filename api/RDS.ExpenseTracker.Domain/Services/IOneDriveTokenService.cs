using RDS.ExpenseTracker.Domain.Models.OneDrive;

namespace RDS.ExpenseTracker.Domain.Services;

/// <summary>
/// Token delegati per il OneDrive personale di ogni utente. Graph non ammette l'accesso app-only ai drive degli
/// account Microsoft personali: l'utente autorizza l'app una volta (device code flow) e il refresh token,
/// che ruota a ogni rinnovo, resta in Key Vault. Implementato in Infrastructure.
/// </summary>
public interface IOneDriveTokenService
{
    /// <summary>Chiede a Microsoft un nuovo codice di accesso da mostrare all'utente.</summary>
    Task<OneDriveDeviceCode> StartDeviceCodeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Prova a riscattare il device code; su <see cref="OneDriveDeviceCodeOutcome.Completed"/> il refresh token
    /// dell'utente è stato salvato.
    /// </summary>
    Task<OneDriveDeviceCodeOutcome> RedeemDeviceCodeAsync(int userId, string deviceCode, CancellationToken cancellationToken = default);

    /// <exception cref="OneDriveAuthorizationException">L'autorizzazione non è più valida.</exception>
    Task<string> GetAccessTokenAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Rende inutilizzabile il refresh token salvato per l'utente.</summary>
    Task RemoveAsync(int userId, CancellationToken cancellationToken = default);
}
