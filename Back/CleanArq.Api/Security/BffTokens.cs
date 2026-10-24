using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace CleanArq.Api.Security;

public static class BffTokens
{
    public static async Task<ClaimsPrincipal> ValidateAccessToken(string token, OpenIdConnectOptions options, string audience, CancellationToken cancellationToken)
    {
        var metadata = options.Configuration ?? await options.ConfigurationManager!.GetConfigurationAsync(cancellationToken);
        return new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(token, new TokenValidationParameters
        {
            ValidIssuer = metadata.Issuer, ValidAudience = audience, IssuerSigningKeys = metadata.SigningKeys,
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30), NameClaimType = "preferred_username", RoleClaimType = ClaimTypes.Role
        }, out _);
    }
    public static ClaimsPrincipal WithRoles(ClaimsPrincipal principal, ClaimsPrincipal accessPrincipal)
    {
        var identity = new ClaimsIdentity(principal.Identity?.AuthenticationType ?? "oidc", "preferred_username", ClaimTypes.Role);
        identity.AddClaims(principal.Claims.Where(c => c.Type != ClaimTypes.Role && c.Type != "realm_access"));
        var value = accessPrincipal.FindFirst("realm_access")?.Value;
        if (value is not null)
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
                foreach (var role in roles.EnumerateArray())
                    if (role.ValueKind == JsonValueKind.String && role.GetString() is { } name)
                        identity.AddClaim(new Claim(ClaimTypes.Role, name));
        }
        return new ClaimsPrincipal(identity);
    }
    public static bool NeedsRefresh(AuthenticationProperties properties) => !DateTimeOffset.TryParse(
        properties.GetTokenValue("expires_at"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiry)
        || expiry <= DateTimeOffset.UtcNow.AddSeconds(30);
}
