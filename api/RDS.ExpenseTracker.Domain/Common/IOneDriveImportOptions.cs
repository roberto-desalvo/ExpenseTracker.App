namespace RDS.ExpenseTracker.Domain.Common;

/// <summary>
/// Configurazione dell'import automatico dei file caricati in una cartella OneDrive personale.
/// </summary>
public interface IOneDriveImportOptions
{
    /// <summary>Se false le function OneDrive non fanno nulla (ambienti senza consenso OneDrive).</summary>
    bool Enabled { get; }

    /// <summary>Client id dell'app registration OneDrive (account Microsoft personali, client pubblico).</summary>
    string ClientId { get; }

    /// <summary>Id della cartella radice osservata (es. <c>/Transazioni</c>), con una sottocartella per banca.</summary>
    string FolderId { get; }

    /// <summary>Email dell'utente ExpenseTracker a cui appartengono i dati importati.</summary>
    string TargetUserEmail { get; }

    /// <summary>
    /// URL pubblico dell'endpoint che riceve le notifiche Graph. Vuoto = ricavato da <c>WEBSITE_HOSTNAME</c>.
    /// </summary>
    string NotificationUrl { get; }

    /// <summary>Segreto condiviso inviato da Graph in ogni notifica, per scartare le chiamate non autentiche.</summary>
    string ClientState { get; }

    /// <summary>Risorsa Graph osservata; vuoto = <c>/me/drive/items/{FolderId}</c>.</summary>
    string SubscriptionResource { get; }

    /// <summary>Vault che contiene il refresh token delegato.</summary>
    string KeyVaultUri { get; }

    /// <summary>Nome del secret con il refresh token (scritto dallo script di consenso e ruotato dalla function).</summary>
    string RefreshTokenSecretName { get; }

    /// <summary>Sottocartella (della cartella della banca) in cui spostare i file importati.</summary>
    string ImportedFolderName { get; }

    /// <summary>Sottocartella (della cartella della banca) in cui spostare i file non importati.</summary>
    string FailedFolderName { get; }
}
