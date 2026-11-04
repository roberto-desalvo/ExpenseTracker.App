using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Common;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace RDS.ExpenseTracker.Infrastructure.OneDrive;

/// <summary>
/// Access token delegato per il OneDrive personale. Graph non supporta l'accesso app-only ai drive
/// degli account Microsoft personali: si usa il refresh token ottenuto una volta con
/// <c>infra/scripts/connect-onedrive.ps1</c> (device code flow) e salvato in Key Vault.
/// Il refresh token ruota a ogni rinnovo: il nuovo valore viene riscritto nel secret.
/// </summary>
public interface IOneDriveTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

public sealed class OneDriveTokenProvider : IOneDriveTokenProvider, IDisposable
{
    public const string HttpClientName = "OneDriveTokenEndpoint";

    // Account Microsoft personali: endpoint "consumers". Client pubblico, nessun secret.
    private const string TokenEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
    private const string Scopes = "Files.ReadWrite offline_access";
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOneDriveImportOptions _options;
    private readonly ILogger<OneDriveTokenProvider> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Lazy<SecretClient> _secretClient;

    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;

    public OneDriveTokenProvider(IHttpClientFactory httpClientFactory, IOneDriveImportOptions options, ILogger<OneDriveTokenProvider> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _secretClient = new Lazy<SecretClient>(() =>
            new SecretClient(new Uri(_options.KeyVaultUri), new DefaultAzureCredential()));
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _accessTokenExpiresAt - ExpiryMargin)
            return _accessToken;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow < _accessTokenExpiresAt - ExpiryMargin)
                return _accessToken;

            var refreshToken = (await _secretClient.Value.GetSecretAsync(_options.RefreshTokenSecretName, cancellationToken: cancellationToken)).Value.Value;
            var token = await RedeemRefreshTokenAsync(refreshToken, cancellationToken);

            if (!string.IsNullOrEmpty(token.RefreshToken) && token.RefreshToken != refreshToken)
            {
                await _secretClient.Value.SetSecretAsync(
                    new KeyVaultSecret(_options.RefreshTokenSecretName, token.RefreshToken)
                    {
                        Properties = { ContentType = "text/plain" },
                    },
                    cancellationToken);
                _logger.LogInformation("OneDrive refresh token rotated and saved to Key Vault");
            }

            _accessToken = token.AccessToken;
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
            return _accessToken!;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<TokenResponse> RedeemRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["scope"] = Scopes,
        });

        using var response = await client.PostAsync(TokenEndpoint, request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            // invalid_grant: refresh token scaduto (90 giorni di inattività) o revocato.
            throw new InvalidOperationException(
                $"OneDrive token refresh failed ({(int)response.StatusCode}): {body}. " +
                "If the grant is invalid, run infra/scripts/connect-onedrive.ps1 again.");
        }

        return await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Empty OneDrive token response.");
    }

    public void Dispose() => _gate.Dispose();

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
