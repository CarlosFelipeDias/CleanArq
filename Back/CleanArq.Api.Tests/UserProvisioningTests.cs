using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CleanArq.Application.Users;
using CleanArq.Infra.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CleanArq.Api.Tests;

public sealed class UserProvisioningTests
{
    private static CreateUserRequest Valid() => new() { Email = "new@example.test", FirstName = "New", LastName = "User", TemporaryPassword = "Temporary-test-only-123!" };
    private static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = JsonContent.Create(body) };
    private static HttpResponseMessage Created() { var response = new HttpResponseMessage(HttpStatusCode.Created); response.Headers.Location = new Uri("https://identity.test/admin/realms/CleanArq/users/new-user"); return response; }
    private static ScriptedHandler Start() => new ScriptedHandler()
        .Then(_ => Json(new { access_token = "test-service-token" }))
        .Then(request => {
            var body = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult()).RootElement;
            Assert.False(body.GetProperty("enabled").GetBoolean());
            Assert.Equal("new@example.test", body.GetProperty("username").GetString());
            Assert.False(body.GetProperty("emailVerified").GetBoolean());
            Assert.True(body.GetProperty("credentials")[0].GetProperty("temporary").GetBoolean());
            Assert.Equal("UPDATE_PASSWORD", body.GetProperty("requiredActions")[0].GetString());
            Assert.False(body.TryGetProperty("realmRoles", out _));
            return Created();
        });
    private static ScriptedHandler WithRole(ScriptedHandler handler) => handler
        .Then(_ => Json(new[] { new { id = "user-role", name = "user", composite = false }, new { id = "admin-role", name = "admin", composite = false } }))
        .Then(request => {
            var roles = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult()).RootElement;
            Assert.Equal(1, roles.GetArrayLength()); Assert.Equal("user", roles[0].GetProperty("name").GetString());
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
    private static async Task<UserCreationResult> Run(ScriptedHandler handler, CreateUserRequest? request = null)
    {
        using var http = new HttpClient(handler);
        var options = Options.Create(new KeycloakProvisioningOptions { ServerUrl = "https://identity.test", Realm = "CleanArq", ClientId = "provisioner", ClientSecret = "fake-test-secret" });
        var provisioner = new KeycloakUserProvisioner(http, options, NullLogger<KeycloakUserProvisioner>.Instance);
        return await new CreateUserHandler(provisioner).HandleAsync(request ?? Valid());
    }
    [Fact]
    public async Task CreatesDisabledAssignsOnlyUserThenEnables()
    {
        var handler = WithRole(Start()).Then(request => {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.True(JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult()).RootElement.GetProperty("enabled").GetBoolean());
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var result = await Run(handler);
        Assert.Equal(UserCreationStatus.Created, result.Status); Assert.Equal("user", result.User!.Role); Assert.Equal(0, handler.Remaining);
    }
    [Fact]
    public async Task DuplicateIsNotModifiedOrActivated()
    {
        var handler = new ScriptedHandler().Then(_ => Json(new { access_token = "test" })).Then(_ => new HttpResponseMessage(HttpStatusCode.Conflict));
        Assert.Equal(UserCreationStatus.Duplicate, (await Run(handler)).Status); Assert.Equal(2, handler.Calls);
    }
    [Fact]
    public async Task FailedRoleAssignmentLeavesAccountDisabled()
    {
        var handler = Start().Then(_ => Json(new[] { new { id = "role", name = "user", composite = false } }))
            .Then(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        var result = await Run(handler);
        Assert.Equal(UserCreationStatus.Incomplete, result.Status); Assert.Equal("disabled", result.AccountState); Assert.Equal("new-user", result.UserId); Assert.Equal(4, handler.Calls);
    }
    [Fact]
    public async Task MissingUserRoleDoesNotActivateAccount()
    {
        var handler = Start().Then(_ => Json(Array.Empty<object>()));
        Assert.Equal(UserCreationStatus.Incomplete, (await Run(handler)).Status); Assert.Equal(3, handler.Calls);
    }
    [Fact]
    public async Task CompositeUserRoleIsRejected()
    {
        var handler = Start().Then(_ => Json(new[] { new { id = "role", name = "user", composite = true } }));
        Assert.Equal(UserCreationStatus.Incomplete, (await Run(handler)).Status); Assert.Equal(3, handler.Calls);
    }
    [Theory]
    [InlineData(true, UserCreationStatus.Created)]
    [InlineData(false, UserCreationStatus.Incomplete)]
    public async Task ActivationTimeoutIsReconciledBeforeReturning(bool enabled, UserCreationStatus expected)
    {
        var handler = WithRole(Start()).Then(_ => throw new TaskCanceledException())
            .Then(_ => Json(new { enabled }));
        Assert.Equal(expected, (await Run(handler)).Status); Assert.Equal(0, handler.Remaining);
    }
    [Fact]
    public async Task FailedActivationAndFailedReconciliationNeverReportSuccess()
    {
        var handler = WithRole(Start()).Then(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError))
            .Then(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var result = await Run(handler);
        Assert.Equal(UserCreationStatus.OutcomeUnknown, result.Status); Assert.Equal("unknown", result.AccountState); Assert.Equal("new-user", result.UserId);
    }
    [Fact]
    public async Task CreationTimeoutNeverRetriesOrTouchesAnExistingAccount()
    {
        var handler = new ScriptedHandler().Then(_ => Json(new { access_token = "test" })).Then(_ => throw new TaskCanceledException());
        Assert.Equal(UserCreationStatus.OutcomeUnknown, (await Run(handler)).Status); Assert.Equal(2, handler.Calls);
    }
    [Fact]
    public async Task MissingLocationDoesNotContinueProvisioning()
    {
        var handler = new ScriptedHandler().Then(_ => Json(new { access_token = "test" })).Then(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var result = await Run(handler);
        Assert.Equal(UserCreationStatus.OutcomeUnknown, result.Status); Assert.Equal("disabled", result.AccountState); Assert.Equal(2, handler.Calls);
    }
    [Fact]
    public async Task InvalidInputNeverCallsIdentityService()
    {
        var handler = new ScriptedHandler(); var request = Valid(); request.FirstName = "   ";
        Assert.Equal(UserCreationStatus.Invalid, (await Run(handler, request)).Status); Assert.Equal(0, handler.Calls);
    }
    [Fact]
    public void RolesCannotBeInjectedIntoRequest()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreateUserRequest>("{\"Email\":\"x@example.test\",\"role\":\"admin\"}"));
    }
    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> steps = new();
        public int Calls { get; private set; }
        public int Remaining => steps.Count;
        public ScriptedHandler Then(Func<HttpRequestMessage, HttpResponseMessage> step) { steps.Enqueue(step); return this; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++; Assert.True(steps.Count > 0, $"Unexpected HTTP operation: {request.Method} {request.RequestUri}");
            return Task.FromResult(steps.Dequeue()(request));
        }
    }
}
