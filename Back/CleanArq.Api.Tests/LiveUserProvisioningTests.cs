using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using CleanArq.Application.Users;
using CleanArq.Infra.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CleanArq.Api.Tests;

public sealed class LiveKeycloakFactAttribute : FactAttribute
{
    public LiveKeycloakFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("CLEANARQ_RUN_KEYCLOAK_SMOKE") != "1")
            Skip = "Opt-in test: requires local Keycloak and the provisioning client secret in API User Secrets.";
    }
}

public sealed class LiveUserProvisioningTests
{
    [LiveKeycloakFact]
    public async Task CreatesOrdinaryUserInRealKeycloakAndRemovesOnlyThatTestAccount()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets(typeof(Program).Assembly, optional: false).Build();
        var secret = configuration["Keycloak:Provisioning:ClientSecret"];
        Assert.False(string.IsNullOrWhiteSpace(secret));
        const string server = "http://localhost:8180";
        const string realm = "CleanArq";
        const string clientId = "cleanarq-user-provisioning";
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        using var tokenResponse = await http.PostAsync($"{server}/realms/{realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials", ["client_id"] = clientId, ["client_secret"] = secret! }));
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        using var tokenBody = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var token = tokenBody.RootElement.GetProperty("access_token").GetString();
        using var adminHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        adminHttp.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var provisioner = new KeycloakUserProvisioner(http, Options.Create(new KeycloakProvisioningOptions
        { ServerUrl = server, Realm = realm, ClientId = clientId, ClientSecret = secret!, AllowHttpForDevelopment = true }), NullLogger<KeycloakUserProvisioner>.Instance);
        var handler = new CreateUserHandler(provisioner);
        var request = new CreateUserRequest
        {
            Email = $"cleanarq-smoke-{Guid.NewGuid():N}@example.test", FirstName = "Disposable", LastName = "Provisioning Test",
            TemporaryPassword = "Aa1!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(20))
        };
        string? id = null;
        try
        {
            var result = await handler.HandleAsync(request);
            id = result.User?.Id ?? result.UserId;
            Assert.True(result.Status == UserCreationStatus.Created, $"Provisioning failed: {result.Status}; {result.Message}; account {id}");
            Assert.Equal("user", result.User!.Role);
            using var userResponse = await adminHttp.GetAsync($"{server}/admin/realms/{realm}/users/{id}");
            userResponse.EnsureSuccessStatusCode();
            using var user = JsonDocument.Parse(await userResponse.Content.ReadAsStringAsync());
            Assert.True(user.RootElement.GetProperty("enabled").GetBoolean());
            Assert.Equal(request.Email, user.RootElement.GetProperty("username").GetString());
            Assert.Contains(user.RootElement.GetProperty("requiredActions").EnumerateArray(), action => action.GetString() == "UPDATE_PASSWORD");
            using var rolesResponse = await adminHttp.GetAsync($"{server}/admin/realms/{realm}/users/{id}/role-mappings/realm/composite");
            rolesResponse.EnsureSuccessStatusCode();
            using var roles = JsonDocument.Parse(await rolesResponse.Content.ReadAsStringAsync());
            var names = roles.RootElement.EnumerateArray().Select(role => role.GetProperty("name").GetString()).ToArray();
            Assert.Contains("user", names); Assert.DoesNotContain("admin", names);
            Assert.Equal(UserCreationStatus.Duplicate, (await handler.HandleAsync(request)).Status);
        }
        finally
        {
            if (id is not null)
            {
                // This ID belongs only to the randomly named account created in this test.
                using var removal = await adminHttp.DeleteAsync($"{server}/admin/realms/{realm}/users/{Uri.EscapeDataString(id)}");
                Assert.Equal(HttpStatusCode.NoContent, removal.StatusCode);
            }
        }
    }
}
