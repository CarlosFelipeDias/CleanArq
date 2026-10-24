using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CleanArq.Application.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArq.Infra.Identity;

public sealed class KeycloakUserProvisioner(HttpClient http, IOptions<KeycloakProvisioningOptions> options,
    ILogger<KeycloakUserProvisioner> logger) : IUserProvisioner
{
    public async Task<UserCreationResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ClientSecret) || string.IsNullOrWhiteSpace(settings.ClientId)
            || string.IsNullOrWhiteSpace(settings.Realm)
            || !Uri.TryCreate(settings.ServerUrl, UriKind.Absolute, out var server)
            || (server.Scheme != Uri.UriSchemeHttps && !(settings.AllowHttpForDevelopment
                && server.Scheme == Uri.UriSchemeHttp && server.IsLoopback)))
            return new(UserCreationStatus.Unavailable, Message: "User provisioning is not configured correctly.");

        var realm = Uri.EscapeDataString(settings.Realm);
        var serverUrl = settings.ServerUrl.TrimEnd('/');
        var usersUrl = $"{serverUrl}/admin/realms/{realm}/users";
        string? userId = null;
        string? token = null;
        var phase = "authorization";
        try
        {
            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post,
                $"{serverUrl}/realms/{realm}/protocol/openid-connect/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials", ["client_id"] = settings.ClientId,
                    ["client_secret"] = settings.ClientSecret
                })
            };
            using var tokenResponse = await http.SendAsync(tokenRequest, cancellationToken);
            if (!tokenResponse.IsSuccessStatusCode)
                return new(UserCreationStatus.Unavailable, Message: "The identity service could not authorize user provisioning.");
            using var tokenBody = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            token = tokenBody.RootElement.GetProperty("access_token").GetString();
            if (string.IsNullOrWhiteSpace(token)) throw new JsonException();

            phase = "creation";
            using var createResponse = await Send(HttpMethod.Post, usersUrl, token, new
            {
                username = request.Email, email = request.Email, firstName = request.FirstName,
                lastName = request.LastName, enabled = false, emailVerified = false,
                requiredActions = new[] { "UPDATE_PASSWORD" },
                credentials = new[] { new { type = "password", value = request.TemporaryPassword, temporary = true } }
            }, cancellationToken);
            if (createResponse.StatusCode == HttpStatusCode.Conflict)
                return new(UserCreationStatus.Duplicate, Message: "A user with this email or username already exists.");
            if (createResponse.StatusCode == HttpStatusCode.BadRequest)
                return new(UserCreationStatus.Invalid, Message: "Keycloak rejected the user details or temporary password. Check the realm's user profile and password policy.");
            if (createResponse.StatusCode != HttpStatusCode.Created)
                return new((int)createResponse.StatusCode >= 500 ? UserCreationStatus.OutcomeUnknown : UserCreationStatus.Unavailable,
                    Message: "The identity service did not confirm account creation. Check Keycloak before retrying.", AccountState: "unknown");
            var location = createResponse.Headers.Location;
            // Never follow the Location host; extract the ID and use our configured server only.
            var locationUri = location is null ? null : new Uri(new Uri(usersUrl + "/"), location);
            userId = locationUri?.Segments.LastOrDefault()?.Trim('/');
            if (string.IsNullOrWhiteSpace(userId))
                return new(UserCreationStatus.OutcomeUnknown, Message: "The disabled account was created, but its identifier was not returned. Check Keycloak before retrying.", AccountState: "disabled");
            var userUrl = $"{usersUrl}/{Uri.EscapeDataString(Uri.UnescapeDataString(userId))}";

            phase = "role assignment";
            using var available = await Send(HttpMethod.Get, $"{userUrl}/role-mappings/realm/available", token, null, cancellationToken);
            if (!available.IsSuccessStatusCode) return Incomplete(userId, "The account is disabled because its user role could not be resolved.");
            using var roles = await JsonDocument.ParseAsync(await available.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var role = roles.RootElement.EnumerateArray().FirstOrDefault(r => r.GetProperty("name").GetString() == "user");
            // Refuse composite roles: they could indirectly grant privileges outside this use case.
            if (role.ValueKind == JsonValueKind.Undefined || role.GetProperty("composite").GetBoolean())
                return Incomplete(userId, "The account is disabled because the ordinary user role is missing or is composite.");
            using var assignment = await Send(HttpMethod.Post, $"{userUrl}/role-mappings/realm", token,
                new[] { new { id = role.GetProperty("id").GetString(), name = "user" } }, cancellationToken);
            if (!assignment.IsSuccessStatusCode) return Incomplete(userId, "The account is disabled because the user role could not be assigned.");

            phase = "activation";
            using var activation = await Send(HttpMethod.Put, userUrl, token, new { enabled = true }, cancellationToken);
            if (!activation.IsSuccessStatusCode)
                return await ReconcileActivation(userUrl, token, userId, request);
            return Created(userId, request);
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            // No exception payloads, passwords, token bodies or secrets are written to logs.
            logger.LogWarning("User provisioning interrupted during {Phase}; account {UserId}; failure type {FailureType}.", phase, userId, exception.GetType().Name);
            if (userId is not null)
            {
                if (phase == "activation" && token is not null)
                {
                    var userUrl = $"{usersUrl}/{Uri.EscapeDataString(Uri.UnescapeDataString(userId))}";
                    return await ReconcileActivation(userUrl, token, userId, request);
                }
                return Incomplete(userId, "The account remains disabled. An administrator must review it in Keycloak before retrying.");
            }
            return phase == "creation"
                ? new(UserCreationStatus.OutcomeUnknown, Message: "Account creation could not be confirmed. Check Keycloak before retrying.", AccountState: "unknown")
                : new(UserCreationStatus.Unavailable, Message: "The identity service is unavailable. Please try again later.");
        }
    }

    private async Task<UserCreationResult> ReconcileActivation(string url, string token, string userId, CreateUserRequest request)
    {
        // The caller may have disconnected; use a fresh bounded token for reconciliation.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            using var response = await Send(HttpMethod.Get, url, token, null, timeout.Token);
            if (response.IsSuccessStatusCode)
            {
                using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(timeout.Token), cancellationToken: timeout.Token);
                if (body.RootElement.TryGetProperty("enabled", out var enabled))
                    return enabled.GetBoolean() ? Created(userId, request)
                        : Incomplete(userId, "The account remains disabled because activation failed. Review it in Keycloak before retrying.");
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException) { }
        logger.LogWarning("Activation outcome is unknown for Keycloak account {UserId}; manual reconciliation is required.", userId);
        return new(UserCreationStatus.OutcomeUnknown, Message: "The account's activation state could not be confirmed. Check Keycloak before retrying.", UserId: userId, AccountState: "unknown");
    }

    private UserCreationResult Incomplete(string userId, string message)
    {
        logger.LogWarning("Keycloak account {UserId} was left disabled after incomplete provisioning.", userId);
        return new(UserCreationStatus.Incomplete, Message: message, UserId: userId, AccountState: "disabled");
    }
    private static UserCreationResult Created(string id, CreateUserRequest request) => new(UserCreationStatus.Created,
        new CreatedUser(Uri.UnescapeDataString(id), request.Email, request.FirstName, request.LastName));
    private async Task<HttpResponseMessage> Send(HttpMethod method, string url, string token, object? body, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) message.Content = JsonContent.Create(body);
        return await http.SendAsync(message, cancellationToken);
    }
}
