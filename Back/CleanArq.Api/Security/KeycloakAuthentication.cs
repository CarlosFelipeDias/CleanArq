using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace CleanArq.Api.Security;

public static class KeycloakAuthentication
{
    public const string CatalogPolicy = "CatalogAccess";
    public const string CookieScheme = "bff-cookie";
    public const string OidcScheme = "keycloak-oidc";
    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var authority = configuration["Keycloak:Authority"] ?? throw new InvalidOperationException("Keycloak:Authority must be configured.");
        var publicUrl = configuration["Bff:PublicOrigin"] ?? throw new InvalidOperationException("Bff:PublicOrigin must be configured.");
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var issuer) || !Uri.TryCreate(publicUrl, UriKind.Absolute, out var origin)
            || ((!environment.IsDevelopment()) && (issuer.Scheme != "https" || origin.Scheme != "https")))
            throw new InvalidOperationException("Keycloak authority and BFF public origin must be absolute HTTPS URLs outside development.");
        var secure = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        services.AddMemoryCache();
        services.AddSingleton<ServerTicketStore>();
        services.AddScoped<BffCookieEvents>();
        services.AddHttpClient("bff-tokens", client => client.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "CleanArq.Antiforgery";
            options.Cookie.HttpOnly = true; options.Cookie.SecurePolicy = secure; options.Cookie.SameSite = SameSiteMode.Strict;
        });
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieScheme;
            options.DefaultSignInScheme = CookieScheme;
            options.DefaultChallengeScheme = CookieScheme;
        }).AddCookie(CookieScheme, options =>
        {
            options.Cookie.Name = "CleanArq.Session";
            options.Cookie.HttpOnly = true; options.Cookie.SecurePolicy = secure; options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30); options.SlidingExpiration = false;
            options.EventsType = typeof(BffCookieEvents);
        }).AddOpenIdConnect(OidcScheme, options =>
        {
            options.Authority = authority;
            options.ClientId = configuration["Keycloak:Bff:ClientId"] ?? "cleanarq-backend";
            options.ClientSecret = configuration["Keycloak:Bff:ClientSecret"];
            options.RequireHttpsMetadata = !environment.IsDevelopment();
            options.ResponseType = OpenIdConnectResponseType.Code; options.ResponseMode = OpenIdConnectResponseMode.Query;
            options.UsePkce = true; options.SaveTokens = true; options.MapInboundClaims = false;
            options.Scope.Clear(); options.Scope.Add("openid"); options.Scope.Add("profile"); options.Scope.Add("email");
            options.GetClaimsFromUserInfoEndpoint = false;
            options.TokenValidationParameters.NameClaimType = "preferred_username";
            options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
            options.CallbackPath = "/signin-oidc"; options.SignedOutCallbackPath = "/signout-callback-oidc";
            options.SignedOutRedirectUri = publicUrl.TrimEnd('/') + "/";
            options.NonceCookie.SameSite = SameSiteMode.Lax; options.NonceCookie.SecurePolicy = secure;
            options.CorrelationCookie.SameSite = SameSiteMode.Lax; options.CorrelationCookie.SecurePolicy = secure;
            options.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProvider = context =>
                {
                    context.ProtocolMessage.RedirectUri = publicUrl.TrimEnd('/') + "/signin-oidc";
                    return Task.CompletedTask;
                },
                OnAuthorizationCodeReceived = context =>
                {
                    // The same redirect_uri must be sent in the server-side code exchange.
                    context.TokenEndpointRequest!.RedirectUri = publicUrl.TrimEnd('/') + "/signin-oidc";
                    return Task.CompletedTask;
                },
                OnRedirectToIdentityProviderForSignOut = context =>
                {
                    context.ProtocolMessage.PostLogoutRedirectUri = publicUrl.TrimEnd('/') + "/signout-callback-oidc";
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var token = context.TokenEndpointResponse?.AccessToken;
                    if (string.IsNullOrWhiteSpace(token)) { context.Fail("An access token is required."); return; }
                    var accessPrincipal = await BffTokens.ValidateAccessToken(token, context.Options, configuration["Keycloak:Audience"]!, context.HttpContext.RequestAborted);
                    if (accessPrincipal.FindFirst("sub")?.Value != context.Principal?.FindFirst("sub")?.Value)
                    { context.Fail("Token subjects do not match."); return; }
                    context.Principal = BffTokens.WithRoles(context.Principal!, accessPrincipal);
                    context.Properties!.Items["session_started"] = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                    context.Properties.IsPersistent = false;
                },
                OnRemoteFailure = context =>
                {
                    context.HandleResponse(); context.Response.Redirect(publicUrl.TrimEnd('/') + "/?authError=signin_failed"); return Task.CompletedTask;
                }
            };
        });
        // SaveTokens is safe only because the complete ticket stays in this server store.
        services.AddOptions<CookieAuthenticationOptions>(CookieScheme).Configure<ServerTicketStore>((options, store) => options.SessionStore = store);
        services.AddAuthorization(options =>
        {
            options.AddPolicy(CatalogPolicy, policy => policy.RequireAuthenticatedUser().RequireRole("admin", "user"));
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        });
        return services;
    }
}
