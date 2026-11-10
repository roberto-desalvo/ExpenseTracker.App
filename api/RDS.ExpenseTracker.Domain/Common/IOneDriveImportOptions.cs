namespace RDS.ExpenseTracker.Domain.Common;

/// <summary>
/// Configurazione dell'import automatico dei file caricati nel OneDrive personale che ogni utente collega dall'app.
/// </summary>
public interface IOneDriveImportOptions
{
    /// <summary>Se false il collegamento non è disponibile e le function OneDrive non fanno nulla.</summary>
    bool Enabled { get; }

    /// <summary>Client id dell'app registration OneDrive (account Microsoft personali, client pubblico).</summary>
    string ClientId { get; }

    /// <summary>
    /// Nome della cartella, nella radice del drive dell'utente, creata al collegamento e poi osservata;
    /// contiene una sottocartella per banca.
    /// </summary>
    string FolderName { get; }

    /// <summary>
    /// URL pubblico dell'endpoint che riceve le notifiche Graph; vuoto se non è noto (sviluppo locale):
    /// in quel caso non si creano subscription e la sincronizzazione è solo manuale.
    /// </summary>
    string NotificationUrl { get; }

    /// <summary>Vault che contiene i refresh token delegati (uno per utente).</summary>
    string KeyVaultUri { get; }

    /// <summary>Prefisso del nome dei secret con i refresh token: <c>&lt;prefisso&gt;-&lt;id utente&gt;</c>.</summary>
    string RefreshTokenSecretName { get; }

    /// <summary>Sottocartella (della cartella della banca) in cui spostare i file importati.</summary>
    string ImportedFolderName { get; }

    /// <summary>Sottocartella (della cartella della banca) in cui spostare i file non importati.</summary>
    string FailedFolderName { get; }
}
