using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using CleanArq.Api.Controllers;
using CleanArq.Api.Security;
using CleanArq.Application.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CleanArq.Api.Tests;

public class AuthenticationTests
{
    [Theory]
    [InlineData("admin", HttpStatusCode.OK)]
    [InlineData("user", HttpStatusCode.OK)]
    [InlineData("viewer", HttpStatusCode.Forbidden)]
    public async Task CatalogRequiresAnAssignedApplicationRole(string role, HttpStatusCode expected)
    { await using var fixture = await Fixture.Create(); await fixture.SignIn(role); Assert.Equal(expected, (await fixture.Client.GetAsync("/catalog")).StatusCode); }
    [Fact]
    public async Task MissingCookieReturns401WithoutLoginRedirect()
    { await using var f = await Fixture.Create(); using var response = await f.Client.GetAsync("/catalog"); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); Assert.Null(response.Headers.Location); }
    [Fact]
    public async Task BearerTokenAloneIsNotABffSession()
    { await using var f = await Fixture.Create(); f.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "unused-token"); Assert.Equal(HttpStatusCode.Unauthorized, (await f.Client.GetAsync("/catalog")).StatusCode); }
    [Theory]
    [InlineData("admin", true, HttpStatusCode.Created)]
    [InlineData("admin", false, HttpStatusCode.BadRequest)]
    [InlineData("user", true, HttpStatusCode.Forbidden)]
    public async Task UserCreationRequiresAdminAndAntiforgery(string role, bool csrf, HttpStatusCode expected)
    {
        await using var f = await Fixture.Create(); await f.SignIn(role); if (csrf) await f.SetCsrf();
        var response = await f.Client.PostAsJsonAsync("/api/users", new CreateUserRequest { Email = "new@example.test", FirstName = "New", LastName = "User", TemporaryPassword = "Temporary-test-only-123!" });
        Assert.Equal(expected, response.StatusCode);
    }
    [Fact]
    public async Task InvalidAntiforgeryIsRejected()
    { await using var f = await Fixture.Create(); await f.SignIn("admin"); f.Client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid"); Assert.Equal(HttpStatusCode.BadRequest, (await f.Client.PostAsync("/bff/logout", null)).StatusCode); }
    [Fact]
    public async Task SessionResponseAndCookieContainNoOAuthTokens()
    {
        await using var f = await Fixture.Create(); await f.SignIn("user");
        var body = await f.Client.GetStringAsync("/bff/session");
        Assert.DoesNotContain("server-only", body); Assert.DoesNotContain("access_token", body); Assert.DoesNotContain("refresh_token", body);
        Assert.Contains("csrfToken", body);
        var ticket = f.CookieTicket(); Assert.Null(ticket.Properties.GetTokenValue("access_token"));
        Assert.Null(ticket.Properties.GetTokenValue("refresh_token"));
        Assert.NotNull(ticket.Principal.FindFirst("Microsoft.AspNetCore.Authentication.Cookies-SessionId"));
    }
    [Fact]
    public async Task RemovedServerSessionCannotBeReused()
    {
        await using var f = await Fixture.Create(); await f.SignIn("user");
        var key = f.CookieTicket().Principal.FindFirst("Microsoft.AspNetCore.Authentication.Cookies-SessionId")!.Value;
        await f.App.Services.GetRequiredService<ServerTicketStore>().RemoveAsync(key);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Client.GetAsync("/catalog")).StatusCode);
    }
    [Fact]
    public async Task LateRefreshCannotRestoreALoggedOutSession()
    {
        await using var f = await Fixture.Create(); await f.SignIn("user");
        var key = f.CookieTicket().Principal.FindFirst("Microsoft.AspNetCore.Authentication.Cookies-SessionId")!.Value;
        var store = f.App.Services.GetRequiredService<ServerTicketStore>();
        var ticket = await store.RetrieveAsync(key);
        await store.RemoveAsync(key); await store.RenewAsync(key, ticket!);
        Assert.Null(await store.RetrieveAsync(key));
    }
    [Fact]
    public async Task ExpiredSessionIsRejected()
    { await using var f = await Fixture.Create(); await f.SignIn("user", expired: true); Assert.Equal(HttpStatusCode.Unauthorized, (await f.Client.GetAsync("/catalog")).StatusCode); }
    [Fact]
    public async Task LogoutRequiresPostAndAntiforgery()
    { await using var f = await Fixture.Create(); await f.SignIn("user"); Assert.Equal(HttpStatusCode.MethodNotAllowed, (await f.Client.GetAsync("/bff/logout")).StatusCode); Assert.Equal(HttpStatusCode.BadRequest, (await f.Client.PostAsync("/bff/logout", null)).StatusCode); }
    [Fact]
    public async Task ValidLogoutRemovesTheLocalSession()
    {
        await using var f = await Fixture.Create(); await f.SignIn("user"); await f.SetCsrf();
        var oldCookie = f.Client.DefaultRequestHeaders.GetValues("Cookie").Single();
        var response = await f.Client.PostAsync("/bff/logout", null); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        f.Client.DefaultRequestHeaders.Remove("Cookie"); f.Client.DefaultRequestHeaders.Add("Cookie", oldCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Client.GetAsync("/catalog")).StatusCode);
    }
    [Fact]
    public async Task RefreshUpdatesRolesAndKeepsTokensServerSide()
    {
        await using var f = await Fixture.Create(refreshRole: "viewer"); await f.SignIn("admin", refreshNeeded: true);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Client.GetAsync("/catalog")).StatusCode);
        Assert.DoesNotContain("access_token", await f.Client.GetStringAsync("/bff/session")); Assert.Equal(1, f.RefreshCalls);
    }
    [Fact]
    public async Task FailedRefreshInvalidatesSession()
    { await using var f = await Fixture.Create(refreshFails: true); await f.SignIn("user", refreshNeeded: true); Assert.Equal(HttpStatusCode.Unauthorized, (await f.Client.GetAsync("/catalog")).StatusCode); }

    [Theory]
    [InlineData("wrong-audience", false, false)]
    [InlineData("cleanarq-backend", true, false)]
    [InlineData("cleanarq-backend", false, true)]
    public async Task InvalidRefreshedAccessTokenIsRejected(string audience, bool expired, bool wrongSignature)
    { await using var f = await Fixture.Create(audience: audience, refreshedExpired: expired, wrongSignature: wrongSignature); await f.SignIn("user", refreshNeeded: true); Assert.Equal(HttpStatusCode.Unauthorized, (await f.Client.GetAsync("/catalog")).StatusCode); }

    private sealed class StubProvisioner : IUserProvisioner
    {
        public Task<UserCreationResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new UserCreationResult(UserCreationStatus.Created, new CreatedUser("test-id", request.Email, request.FirstName, request.LastName)));
    }
    private sealed class Fixture : IAsyncDisposable
    {
        public required WebApplication App { get; init; }
        public required HttpClient Client { get; init; }
        public int RefreshCalls;
        private readonly Dictionary<string, string> cookies = new();
        public static async Task<Fixture> Create(string refreshRole = "user", bool refreshFails = false, string audience = "cleanarq-backend", bool refreshedExpired = false, bool wrongSignature = false)
        {
            using var rsa = RSA.Create(2048); using var other = RSA.Create(2048);
            var key = new RsaSecurityKey(rsa) { KeyId = "test-key" };
            const string issuer = "https://identity.test/realms/CleanArq";
            var jwt = new JwtSecurityToken(issuer, audience, new[] { new Claim("sub", "subject"), new Claim("realm_access", JsonSerializer.Serialize(new { roles = new[] { refreshRole } }), JsonClaimValueTypes.Json) },
                DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(refreshedExpired ? -2 : 5),
                new SigningCredentials(wrongSignature ? new RsaSecurityKey(other) { KeyId = key.KeyId } : key, SecurityAlgorithms.RsaSha256));
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
            var publicKey = new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = key.KeyId };
            var builder = WebApplication.CreateBuilder(); builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Keycloak:Authority"] = issuer, ["Keycloak:Audience"] = "cleanarq-backend", ["Keycloak:Bff:ClientSecret"] = "test-secret", ["Bff:PublicOrigin"] = "https://frontend.test" });
            builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);
            builder.Services.PostConfigure<OpenIdConnectOptions>(KeycloakAuthentication.OidcScheme, options =>
            {
                var metadata = new OpenIdConnectConfiguration { Issuer = issuer, TokenEndpoint = issuer + "/token", EndSessionEndpoint = issuer + "/logout", AuthorizationEndpoint = issuer + "/authorize" };
                metadata.SigningKeys.Add(publicKey); options.Configuration = metadata; options.ConfigurationManager = new Microsoft.IdentityModel.Protocols.StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
            });
            Fixture? fixture = null;
            builder.Services.AddHttpClient("bff-tokens").ConfigurePrimaryHttpMessageHandler(() => new RefreshHandler(() => { fixture!.RefreshCalls++; return refreshFails ? new HttpResponseMessage(HttpStatusCode.BadRequest) : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { access_token = accessToken, refresh_token = "server-only-next-refresh", expires_in = 300 }) }; }));
            builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute())).AddApplicationPart(typeof(UsersController).Assembly);
            builder.Services.AddScoped<CreateUserHandler>(); builder.Services.AddSingleton<IUserProvisioner, StubProvisioner>();
            var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
            app.MapGet("/catalog", () => "catalog").RequireAuthorization(KeycloakAuthentication.CatalogPolicy);
            // This fake sign-in endpoint exists only in the in-memory test host.
            app.MapGet("/test/signin", async context =>
            {
                var claims = new[] { new Claim("sub", "subject"), new Claim("preferred_username", "test-user"), new Claim(ClaimTypes.Role, context.Request.Query["role"].ToString()) };
                var properties = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(context.Request.Query.ContainsKey("expired") ? -1 : 30) };
                properties.Items["session_started"] = DateTimeOffset.UtcNow.ToString("o");
                properties.StoreTokens(new[] { new AuthenticationToken { Name = "access_token", Value = "server-only-access" }, new AuthenticationToken { Name = "refresh_token", Value = "server-only-refresh" }, new AuthenticationToken { Name = "id_token", Value = "server-only-id" }, new AuthenticationToken { Name = "expires_at", Value = DateTimeOffset.UtcNow.AddMinutes(context.Request.Query.ContainsKey("refresh") ? -1 : 5).ToString("o", CultureInfo.InvariantCulture) } });
                await context.SignInAsync(KeycloakAuthentication.CookieScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, "test", "preferred_username", ClaimTypes.Role)), properties);
            }).AllowAnonymous();
            await app.StartAsync(); var client = app.GetTestClient(); client.BaseAddress = new Uri("https://frontend.test");
            fixture = new Fixture { App = app, Client = client }; return fixture;
        }
        public async Task SignIn(string role, bool expired = false, bool refreshNeeded = false)
        { using var response = await Client.GetAsync($"/test/signin?role={role}{(expired ? "&expired=1" : "")}{(refreshNeeded ? "&refresh=1" : "")}"); SetCookies(response); }
        private void SetCookies(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var values)) foreach (var value in values)
            { var pair = value.Split(';')[0].Split('=', 2); cookies[pair[0]] = pair[1]; }
            Client.DefaultRequestHeaders.Remove("Cookie"); Client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies.Select(c => c.Key + "=" + c.Value)));
        }
        public async Task SetCsrf()
        {
            using var response = await Client.GetAsync("/bff/session"); SetCookies(response);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); Client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", json.RootElement.GetProperty("csrfToken").GetString());
        }
        public AuthenticationTicket CookieTicket() => App.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(KeycloakAuthentication.CookieScheme).TicketDataFormat.Unprotect(cookies["CleanArq.Session"])!;
        public async ValueTask DisposeAsync() { Client.Dispose(); await App.DisposeAsync(); }
    }
    private sealed class RefreshHandler(Func<HttpResponseMessage> response) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response()); }
}
