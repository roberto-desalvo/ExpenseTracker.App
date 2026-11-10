using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Models.OneDrive;
using RDS.ExpenseTracker.Domain.Services;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RDS.ExpenseTracker.Infrastructure.OneDrive;

/// <summary>
/// Token delegati per i OneDrive personali, con il client pubblico (nessun secret) dell'app registration OneDrive.
/// Il refresh token di ogni utente è un secret di Key Vault (<c>&lt;prefisso&gt;-&lt;id utente&gt;</c>) e ruota a
/// ogni rinnovo: il nuovo valore viene riscritto nel secret. Gli access token restano in memoria fino alla scadenza.
/// </summary>
public sealed class OneDriveTokenService : IOneDriveTokenService, IDisposable
{
    public const string HttpClientName = "OneDriveTokenEndpoint";

    // Account Microsoft personali: endpoint "consumers".
    private const string Authority = "https://login.microsoftonline.com/consumers/oauth2/v2.0";
    private const string Scopes = "Files.ReadWrite offline_access";
    private const string DeviceCodeGrantType = "urn:ietf:params:oauth:grant-type:device_code";

    // Valore scritto nel secret quando l'utente scollega OneDrive: con il soft delete di Key Vault un secret
    // eliminato non potrebbe essere ricreato con lo stesso nome a un nuovo collegamento.
    private const string RevokedMarker = "revoked";

    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOneDriveImportOptions _options;
    private readonly ILogger<OneDriveTokenService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Lazy<SecretClient> _secretClient;
    private readonly ConcurrentDictionary<int, CachedToken> _accessTokens = new();

    public OneDriveTokenService(IHttpClientFactory httpClientFactory, IOneDriveImportOptions options, ILogger<OneDriveTokenService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _secretClient = new Lazy<SecretClient>(() =>
            new SecretClient(new Uri(_options.KeyVaultUri), new DefaultAzureCredential()));
    }

    public async Task<OneDriveDeviceCode> StartDeviceCodeAsync(CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["scope"] = Scopes,
        });

        using var response = await client.PostAsync($"{Authority}/devicecode", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"OneDrive device code request failed ({(int)response.StatusCode}): {body}");
        }

        var code = await response.Content.ReadFromJsonAsync<DeviceCodeResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Empty OneDrive device code response.");

        return new OneDriveDeviceCode(
            code.DeviceCode,
            code.UserCode,
            code.VerificationUri,
            DateTimeOffset.UtcNow.AddSeconds(code.ExpiresIn),
            Math.Max(code.Interval, 5));
    }

    public async Task<OneDriveDeviceCodeOutcome> RedeemDeviceCodeAsync(int userId, string deviceCode, CancellationToken cancellationToken = default)
    {
        var (token, error) = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["grant_type"] = DeviceCodeGrantType,
            ["device_code"] = deviceCode,
        }, cancellationToken);

        if (token is null)
        {
            return error!.Error switch
            {
                "authorization_pending" or "slow_down" => OneDriveDeviceCodeOutcome.Pending,
                "authorization_declined" => OneDriveDeviceCodeOutcome.Declined,
                // invalid_grant: codice già riscattato da un tentativo precedente
                "expired_token" or "bad_verification_code" or "invalid_grant" => OneDriveDeviceCodeOutcome.Expired,
                _ => throw new InvalidOperationException($"OneDrive device code redemption failed: {error.Error} {error.ErrorDescription}"),
            };
        }

        if (string.IsNullOrEmpty(token.RefreshToken))
            throw new InvalidOperationException("OneDrive sign-in returned no refresh token (offline_access not granted).");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await SaveRefreshTokenAsync(userId, token.RefreshToken, cancellationToken);
            Cache(userId, token);
        }
        finally
        {
            _gate.Release();
        }

        return OneDriveDeviceCodeOutcome.Completed;
    }

    public async Task<string> GetAccessTokenAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (TryGetCached(userId, out var cached))
            return cached;

        // Un solo rinnovo alla volta: il refresh token ruota e due rinnovi concorrenti si invaliderebbero a vicenda.
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (TryGetCached(userId, out cached))
                return cached;

            var refreshToken = await ReadRefreshTokenAsync(userId, cancellationToken)
                ?? throw new OneDriveAuthorizationException("No OneDrive authorization stored for this user.");

            var (token, error) = await RequestTokenAsync(new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["scope"] = Scopes,
            }, cancellationToken);

            if (token is null)
            {
                // invalid_grant: refresh token scaduto (90 giorni di inattività) o accesso revocato dall'utente.
                if (error!.Error is "invalid_grant" or "interaction_required")
                    throw new OneDriveAuthorizationException($"OneDrive authorization is no longer valid: {error.ErrorDescription}");

                throw new InvalidOperationException($"OneDrive token refresh failed: {error.Error} {error.ErrorDescription}");
            }

            if (!string.IsNullOrEmpty(token.RefreshToken) && token.RefreshToken != refreshToken)
            {
                await SaveRefreshTokenAsync(userId, token.RefreshToken, cancellationToken);
                _logger.LogInformation("OneDrive refresh token of user {UserId} rotated and saved to Key Vault", userId);
            }

            return Cache(userId, token);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(int userId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _accessTokens.TryRemove(userId, out _);
            if (await ReadRefreshTokenAsync(userId, cancellationToken) is not null)
                await SaveRefreshTokenAsync(userId, RevokedMarker, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private string SecretName(int userId) => $"{_options.RefreshTokenSecretName}-{userId}";

    private async Task<string?> ReadRefreshTokenAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            var value = (await _secretClient.Value.GetSecretAsync(SecretName(userId), cancellationToken: cancellationToken)).Value.Value;
            return string.IsNullOrEmpty(value) || value == RevokedMarker ? null : value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    private Task SaveRefreshTokenAsync(int userId, string value, CancellationToken cancellationToken)
        => _secretClient.Value.SetSecretAsync(
            new KeyVaultSecret(SecretName(userId), value) { Properties = { ContentType = "text/plain" } },
            cancellationToken);

    private bool TryGetCached(int userId, out string accessToken)
    {
        if (_accessTokens.TryGetValue(userId, out var cached) && DateTimeOffset.UtcNow < cached.ExpiresAt - ExpiryMargin)
        {
            accessToken = cached.AccessToken;
            return true;
        }

        accessToken = string.Empty;
        return false;
    }

    private string Cache(int userId, TokenResponse token)
    {
        _accessTokens[userId] = new CachedToken(token.AccessToken, DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn));
        return token.AccessToken;
    }

    private async Task<(TokenResponse? Token, TokenError? Error)> RequestTokenAsync(
        Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new FormUrlEncodedContent(form);
        using var response = await client.PostAsync($"{Authority}/token", request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Empty OneDrive token response.");
            return (token, null);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            if (JsonSerializer.Deserialize<TokenError>(body) is { Error: not null } error)
                return (null, error);
        }
        catch (JsonException)
        {
            // corpo non JSON: gestito sotto
        }

        throw new InvalidOperationException($"OneDrive token request failed ({(int)response.StatusCode}): {body}");
    }

    private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresAt);

    private sealed record DeviceCodeResponse(
        [property: JsonPropertyName("device_code")] string DeviceCode,
        [property: JsonPropertyName("user_code")] string UserCode,
        [property: JsonPropertyName("verification_uri")] string VerificationUri,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("interval")] int Interval);

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private sealed record TokenError(
        [property: JsonPropertyName("error")] string? Error,
        [property: JsonPropertyName("error_description")] string? ErrorDescription);
}
