using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace CleanArq.Api.Security;

public static class KeycloakAuthentication
{
    public const string CatalogPolicy = "CatalogAccess";

    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        var authority = configuration["Keycloak:Authority"]
            ?? throw new InvalidOperationException("Keycloak:Authority must be configured.");
        var audience = configuration["Keycloak:Audience"]
            ?? throw new InvalidOperationException("Keycloak:Audience must be configured.");
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri)
            || (!environment.IsDevelopment() && authorityUri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("Keycloak:Authority must be an absolute HTTPS URL outside development.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.RequireHttpsMetadata = !environment.IsDevelopment();
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                NameClaimType = "preferred_username",
                RoleClaimType = ClaimTypes.Role,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    // Keycloak realm roles are nested JSON; ASP.NET role checks expect individual claims.
                    if (context.Principal?.Identity is ClaimsIdentity identity)
                        AddRealmRoles(identity);
                    return Task.CompletedTask;
                }
            };
        });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(CatalogPolicy, policy => policy.RequireAuthenticatedUser().RequireRole("admin", "user"));
            // New controller endpoints are protected even if an attribute is accidentally omitted.
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        });
        return services;
    }

    internal static void AddRealmRoles(ClaimsIdentity identity)
    {
        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (realmAccess is null) return;
        try
        {
            using var document = JsonDocument.Parse(realmAccess);
            if (!document.RootElement.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array) return;
            foreach (var role in roles.EnumerateArray())
                if (role.ValueKind == JsonValueKind.String && role.GetString() is { } name
                    && !identity.HasClaim(ClaimTypes.Role, name))
                    identity.AddClaim(new Claim(ClaimTypes.Role, name));
        }
        catch (JsonException) { /* Malformed roles grant no catalog permissions. */ }
    }
}
