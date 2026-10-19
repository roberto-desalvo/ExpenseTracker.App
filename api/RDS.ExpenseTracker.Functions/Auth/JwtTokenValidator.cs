using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using RDS.ExpenseTracker.Functions.Options;

namespace RDS.ExpenseTracker.Functions.Auth;

/// <summary>
/// Validates Entra ID (Azure AD) bearer tokens issued for this API.
/// Replaces the ASP.NET Core JwtBearer / Microsoft.Identity.Web pipeline, which is not available in the isolated worker.
/// </summary>
public class JwtTokenValidator
{
    private const string DefaultInstance = "https://login.microsoftonline.com/";

    private readonly AzureAdOptions _options;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly JsonWebTokenHandler _tokenHandler = new() { MapInboundClaims = true };

    public JwtTokenValidator(IOptions<AzureAdOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        var instance = string.IsNullOrWhiteSpace(_options.Instance) ? DefaultInstance : _options.Instance;
        var metadataAddress = $"{instance.TrimEnd('/')}/{_options.TenantId}/v2.0/.well-known/openid-configuration";

        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        var configuration = await _configurationManager.GetConfigurationAsync(cancellationToken);

        var parameters = new TokenValidationParameters
        {
            ValidIssuers =
            [
                $"https://login.microsoftonline.com/{_options.TenantId}/v2.0",
                $"https://sts.windows.net/{_options.TenantId}/"
            ],
            ValidAudiences = GetValidAudiences(),
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        var result = await _tokenHandler.ValidateTokenAsync(token, parameters);
        if (!result.IsValid)
        {
            // Signing keys may have been rotated: refresh metadata once on the next request.
            if (result.Exception is SecurityTokenSignatureKeyNotFoundException)
                _configurationManager.RequestRefresh();

            return null;
        }

        return new ClaimsPrincipal(result.ClaimsIdentity);
    }

    private IEnumerable<string> GetValidAudiences()
    {
        if (!string.IsNullOrWhiteSpace(_options.Audience))
            yield return _options.Audience;

        if (!string.IsNullOrWhiteSpace(_options.ClientId))
        {
            yield return _options.ClientId;
            yield return $"api://{_options.ClientId}";
        }
    }
}
