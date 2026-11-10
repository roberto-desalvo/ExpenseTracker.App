using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Functions.Options;

/// <summary>
/// Import automatico dei file caricati su OneDrive (sezione <c>OneDriveImport</c>, app settings <c>OneDriveImport__*</c>).
/// </summary>
public class OneDriveImportOptions : IAppOptions, IOneDriveImportOptions
{
    public const string SectionName = "OneDriveImport";

    private string _notificationUrl = string.Empty;

    /// <inheritdoc />
    string IAppOptions.SectionName => SectionName;

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <inheritdoc />
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// URL pubblico dell'endpoint delle notifiche Graph. Se non impostato si ricava da <c>WEBSITE_HOSTNAME</c>
    /// (nome host pubblico della Function App, impostato dalla piattaforma); in locale resta vuoto.
    /// </summary>
    public string NotificationUrl
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_notificationUrl))
                return _notificationUrl;

            var hostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
            return string.IsNullOrWhiteSpace(hostName) || hostName.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : $"https://{hostName}/api/onedrive/notifications";
        }
        set => _notificationUrl = value ?? string.Empty;
    }

    /// <inheritdoc />
    public string KeyVaultUri { get; set; } = string.Empty;

    /// <inheritdoc />
    public string RefreshTokenSecretName { get; set; } = "OneDriveRefreshToken";

    /// <inheritdoc />
    public string ImportedFolderName { get; set; } = "Importati";

    /// <inheritdoc />
    public string FailedFolderName { get; set; } = "Errori";
}
