using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using CleanArq.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
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
    [InlineData(null, HttpStatusCode.Forbidden)]
    public async Task CatalogRequiresAnAssignedApplicationRole(string? role, HttpStatusCode expected)
        => Assert.Equal(expected, await Request(role: role));

    [Fact]
    public async Task MissingTokenIsUnauthorized()
        => Assert.Equal(HttpStatusCode.Unauthorized, await Request(noToken: true));

    [Fact]
    public async Task WrongAudienceIsUnauthorized()
        => Assert.Equal(HttpStatusCode.Unauthorized, await Request(audience: "another-api"));

    [Fact]
    public async Task WrongIssuerIsUnauthorized()
        => Assert.Equal(HttpStatusCode.Unauthorized, await Request(issuer: "https://another-issuer.test"));

    [Fact]
    public async Task ExpiredTokenIsUnauthorized()
        => Assert.Equal(HttpStatusCode.Unauthorized, await Request(expired: true));

    [Fact]
    public async Task WrongSignatureIsUnauthorized()
        => Assert.Equal(HttpStatusCode.Unauthorized, await Request(wrongSignature: true));

    private static async Task<HttpStatusCode> Request(string? role = "user", bool noToken = false,
        string audience = "cleanarq-backend", string issuer = "https://identity.test/realms/CleanArq",
        bool expired = false, bool wrongSignature = false)
    {
        const string authority = "https://identity.test/realms/CleanArq";
        using var rsa = RSA.Create(2048);
        using var otherRsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test-signing-key" };
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["Keycloak:Authority"] = authority, ["Keycloak:Audience"] = "cleanarq-backend" });
        builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);
        builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var metadata = new OpenIdConnectConfiguration { Issuer = authority };
            metadata.SigningKeys.Add(key);
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
        });
        await using var app = builder.Build();
        app.UseAuthentication(); app.UseAuthorization();
        app.MapGet("/catalog", () => "catalog").RequireAuthorization(KeycloakAuthentication.CatalogPolicy);
        await app.StartAsync();
        using var client = app.GetTestClient();
        if (!noToken)
        {
            var claims = new List<Claim> { new("sub", "test-subject") };
            if (role is not null) claims.Add(new Claim("realm_access", JsonSerializer.Serialize(new { roles = new[] { role } }), JsonClaimValueTypes.Json));
            var token = new JwtSecurityToken(issuer, audience, claims,
                DateTime.UtcNow.AddMinutes(-10), expired ? DateTime.UtcNow.AddMinutes(-2) : DateTime.UtcNow.AddMinutes(5),
                new SigningCredentials(wrongSignature ? new RsaSecurityKey(otherRsa) { KeyId = key.KeyId } : key, SecurityAlgorithms.RsaSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        }
        using var response = await client.GetAsync("/catalog");
        return response.StatusCode;
    }
}
