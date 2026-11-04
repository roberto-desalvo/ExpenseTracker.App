using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Functions.Options;

/// <summary>
/// Import automatico dei file caricati su OneDrive (sezione <c>OneDriveImport</c>, app settings <c>OneDriveImport__*</c>).
/// </summary>
public class OneDriveImportOptions : IAppOptions, IOneDriveImportOptions
{
    public const string SectionName = "OneDriveImport";

    /// <inheritdoc />
    string IAppOptions.SectionName => SectionName;

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <inheritdoc />
    public string ClientId { get; set; } = string.Empty;

    /// <inheritdoc />
    public string FolderId { get; set; } = string.Empty;

    /// <inheritdoc />
    public string TargetUserEmail { get; set; } = string.Empty;

    /// <inheritdoc />
    public string NotificationUrl { get; set; } = string.Empty;

    /// <inheritdoc />
    public string ClientState { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SubscriptionResource { get; set; } = string.Empty;

    /// <inheritdoc />
    public string KeyVaultUri { get; set; } = string.Empty;

    /// <inheritdoc />
    public string RefreshTokenSecretName { get; set; } = "OneDriveRefreshToken";

    /// <inheritdoc />
    public string ImportedFolderName { get; set; } = "Importati";

    /// <inheritdoc />
    public string FailedFolderName { get; set; } = "Errori";
}
