using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace CleanArq.Api.Security;

public sealed class BffCookieEvents(ServerTicketStore store, IHttpClientFactory clients,
    IOptionsMonitor<OpenIdConnectOptions> oidc, IConfiguration configuration) : CookieAuthenticationEvents
{
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; }
    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; }
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (!context.Properties.Items.TryGetValue(ServerTicketStore.SessionId, out var key) || key is null
            || !context.Properties.Items.TryGetValue("session_started", out var startedText)
            || !DateTimeOffset.TryParse(startedText, out var started)
            || DateTimeOffset.UtcNow - started >= TimeSpan.FromHours(8))
        { await Reject(context, key); return; }
        if (!BffTokens.NeedsRefresh(context.Properties)) return;
        var gate = store.RefreshLock(key);
        await gate.WaitAsync(context.HttpContext.RequestAborted);
        try
        {
            var latest = await store.RetrieveAsync(key);
            if (latest is null) { await Reject(context, key); return; }
            if (!BffTokens.NeedsRefresh(latest.Properties))
            {
                context.ReplacePrincipal(latest.Principal);
                context.Properties.StoreTokens(latest.Properties.GetTokens());
                context.Properties.ExpiresUtc = latest.Properties.ExpiresUtc;
                context.ShouldRenew = true;
                return;
            }
            var options = oidc.Get(KeycloakAuthentication.OidcScheme);
            var metadata = options.Configuration ?? await options.ConfigurationManager!.GetConfigurationAsync(context.HttpContext.RequestAborted);
            var refresh = latest.Properties.GetTokenValue("refresh_token");
            if (string.IsNullOrWhiteSpace(refresh)) { await Reject(context, key); return; }
            using var response = await clients.CreateClient("bff-tokens").PostAsync(metadata.TokenEndpoint,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token", ["refresh_token"] = refresh,
                    ["client_id"] = options.ClientId!, ["client_secret"] = options.ClientSecret!
                }), context.HttpContext.RequestAborted);
            if (!response.IsSuccessStatusCode) { await Reject(context, key); return; }
            using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(context.HttpContext.RequestAborted), cancellationToken: context.HttpContext.RequestAborted);
            var access = body.RootElement.GetProperty("access_token").GetString()!;
            var accessPrincipal = await BffTokens.ValidateAccessToken(access, options, configuration["Keycloak:Audience"]!, context.HttpContext.RequestAborted);
            // A refreshed token must still identify the same user.
            if (accessPrincipal.FindFirst("sub")?.Value != latest.Principal.FindFirst("sub")?.Value) { await Reject(context, key); return; }
            var principal = BffTokens.WithRoles(latest.Principal, accessPrincipal);
            var tokens = latest.Properties.GetTokens().ToDictionary(t => t.Name, t => t.Value);
            tokens["access_token"] = access;
            if (body.RootElement.TryGetProperty("refresh_token", out var nextRefresh)) tokens["refresh_token"] = nextRefresh.GetString()!;
            if (body.RootElement.TryGetProperty("id_token", out var idToken)) tokens["id_token"] = idToken.GetString()!;
            tokens["expires_at"] = DateTimeOffset.UtcNow.AddSeconds(body.RootElement.GetProperty("expires_in").GetInt32()).ToString("o", CultureInfo.InvariantCulture);
            latest.Properties.StoreTokens(tokens.Select(t => new AuthenticationToken { Name = t.Key, Value = t.Value }));
            latest.Properties.ExpiresUtc = new[] { DateTimeOffset.UtcNow.AddMinutes(30), started.AddHours(8) }.Min();
            await store.RenewAsync(key, new AuthenticationTicket(principal, latest.Properties, KeycloakAuthentication.CookieScheme));
            context.ReplacePrincipal(principal);
            context.Properties.StoreTokens(latest.Properties.GetTokens());
            context.Properties.ExpiresUtc = latest.Properties.ExpiresUtc;
            context.ShouldRenew = true;
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or JsonException
            or Microsoft.IdentityModel.Tokens.SecurityTokenException or InvalidOperationException or KeyNotFoundException)
        { await Reject(context, key); }
        finally { gate.Release(); }
    }
    private async Task Reject(CookieValidatePrincipalContext context, string? key)
    {
        context.RejectPrincipal();
        if (key is not null) await store.RemoveAsync(key);
        await context.HttpContext.SignOutAsync(KeycloakAuthentication.CookieScheme);
    }
}
