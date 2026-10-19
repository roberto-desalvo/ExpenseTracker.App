using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RDS.ExpenseTracker.Functions.Options;

namespace RDS.ExpenseTracker.Functions.Auth;

/// <summary>
/// Issues and validates the bearer tokens of the demo login. Tokens are signed with a symmetric key
/// held only by the API, carry no roles and identify the user by email.
/// </summary>
public class DemoTokenService
{
    /// <summary>Authentication type of the principals created from demo tokens.</summary>
    public const string AuthenticationType = "DemoUser";

    public const string DemoUserName = "Demo";

    private const string Issuer = "rds-expense-tracker-demo";
    private const string Audience = "rds-expense-tracker-api";
    private const int MinimumKeyBytes = 32;

    private readonly DemoAuthOptions _options;
    private readonly SymmetricSecurityKey? _signingKey;
    private readonly JsonWebTokenHandler _tokenHandler = new() { MapInboundClaims = false };

    public DemoTokenService(IOptions<DemoAuthOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (_options.Enabled)
        {
            EnsureValid(_options);
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        }
    }

    public bool IsEnabled => _signingKey is not null;

    /// <summary>Fails fast at startup when the demo login is enabled with an unusable configuration.</summary>
    public static void EnsureValid(DemoAuthOptions options)
    {
        if (!options.Enabled)
            return;

        if (Encoding.UTF8.GetByteCount(options.SigningKey ?? string.Empty) < MinimumKeyBytes)
            throw new InvalidOperationException($"DemoAuth:SigningKey must be at least {MinimumKeyBytes} bytes when DemoAuth:Enabled is true.");

        if (options.TokenLifetimeMinutes is < 1 or > 24 * 60)
            throw new InvalidOperationException("DemoAuth:TokenLifetimeMinutes must be between 1 and 1440.");
    }

    /// <summary>True when the token was issued by the demo login (signature not checked here).</summary>
    public bool IsDemoToken(string token)
    {
        if (!IsEnabled || !_tokenHandler.CanReadToken(token))
            return false;

        try
        {
            return _tokenHandler.ReadJsonWebToken(token).Issuer == Issuer;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public (string AccessToken, DateTime ExpiresOn) CreateToken(string email)
    {
        if (_signingKey is null)
            throw new InvalidOperationException("Demo login is not enabled.");

        var now = DateTime.UtcNow;
        var expiresOn = now.AddMinutes(_options.TokenLifetimeMinutes);

        var token = _tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresOn,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = $"demo:{email}",
                ["email"] = email,
                ["name"] = DemoUserName,
            },
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
        });

        return (token, expiresOn);
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(string token)
    {
        if (_signingKey is null)
            return null;

        var result = await _tokenHandler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = _signingKey,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            AuthenticationType = AuthenticationType,
            NameClaimType = "name",
            RoleClaimType = ClaimTypes.Role,
        });

        return result.IsValid ? new ClaimsPrincipal(result.ClaimsIdentity) : null;
    }
}
