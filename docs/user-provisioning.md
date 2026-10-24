# Ordinary user creation

## Scope and layers

POST /api/users creates an ordinary account in the CleanArq Keycloak realm. It does not create a MySQL record or a Domain User entity. UsersController requires the application realm role admin. The Application CreateUserHandler normalizes and validates input, then calls IUserProvisioner. CleanArq.Infra.Identity implements the Keycloak HTTP workflow. IoC registers the typed HttpClient, options and handler.

The request accepts only email, firstName, lastName and temporaryPassword. Unknown properties (including role, roles, enabled and emailVerified) are rejected. Email is also the username. Names and email are trimmed; passwords are never trimmed. Temporary passwords must be 8–128 characters and satisfy any additional Keycloak password policy. The response never includes the password.

## Workflow

1. Obtain a service-account access token using client_credentials. The caller's browser session is NOT forwarded to Keycloak administration.
2. Create the account with enabled=false, emailVerified=false, a temporary password and requiredActions=[UPDATE_PASSWORD].
3. Resolve the non-composite realm role named user from available user role mappings.
4. Assign that role. There is no role selector in the request or arbitrary-role operation.
5. Enable the account only after assignment succeeds.
6. Return 201 and the Keycloak ID, email, names and role.

Keycloak may add its realm-default technical roles. The application assigns only user; it does not remove built-in defaults or modify existing accounts.

## Partial failures and retries

There is no database transaction spanning Keycloak REST calls. If role lookup or assignment fails, the account remains disabled and the response includes its userId and accountState=disabled. Do not repeat the POST blindly: inspect the account in Keycloak, repair or remove it manually, then decide whether to retry.

If enabling times out or returns an error, the integration reads the account with an independent five-second timeout. It returns success only if enabled=true is confirmed. Confirmed disabled accounts return an incomplete failure. If state cannot be read, the response is OutcomeUnknown with accountState=unknown; it does NOT claim the account is disabled or created successfully.

If account creation itself times out, the server may have created a disabled account even though no response reached the API. No automatic retry, search-based takeover or deletion is attempted. Check Keycloak by email first. Duplicate accounts return 409 and are not modified.

If the requesting browser disconnects, cancellation may prevent receiving the response even if provisioning completed. Check Keycloak before retrying in this situation too. No durable background worker or automatic recovery queue is implemented in this local scope.

## Status codes

- 201: creation, role assignment and activation confirmed.
- 400: invalid input, unknown fields or rejected Keycloak user/password policy.
- 401: caller is not authenticated.
- 403: caller is authenticated but lacks admin.
- 409: duplicate email/username.
- 502: partial provisioning or an uncertain remote outcome; inspect code, userId and accountState.
- 503: missing integration configuration, service credential rejection or unavailable identity service before creation.

Failure bodies use ProblemDetails, with code and traceId. They do not include upstream response bodies, passwords or tokens. Logs include the provisioning phase and account ID where known, without credential payloads.

## Keycloak service client

Client: cleanarq-user-provisioning. Enable client authentication and service-account roles; keep Standard Flow, Direct Access Grants and Implicit Flow disabled. No interactive redirect URL is needed. Grant only realm-management/manage-users for this local setup, not realm-admin or master-realm permissions.

IMPORTANT: manage-users is broader than create-only: the service credential can manage existing realm users. Restricting creation to user is enforced by the API contract and workflow, not by a claim that this coarse Keycloak permission itself only permits user creation. Fine-grained admin delegation is a separate hardening task, with version-specific configuration (this installation is Keycloak 26.0). Never expose this service secret or service token in React.

## Configuration

Non-secret development settings are under Keycloak:Provisioning in appsettings.Development.json. The secret must be stored in the API's existing User Secrets store:

    dotnet user-secrets set "Keycloak:Provisioning:ClientSecret" "<client-secret>" --project Back/CleanArq.Api

Do not commit the secret. In deployed environments use the configured secret provider or Keycloak__Provisioning__ClientSecret. HTTPS is required outside Development. The development HTTP exception is restricted to loopback servers and is overridden off outside Development.

Missing credentials do not prevent catalog authentication from starting; only provisioning returns 503 until configured. Tokens are requested per provisioning operation and are not persisted. No automatic retry is configured for POST operations.

## Tests

    dotnet test Back/CleanArq.Api.Tests/CleanArq.Api.Tests.csproj --artifacts-path .artifacts

Tests cover actual endpoint authorization (admin accepted, user forbidden, missing token unauthorized), input validation, role injection rejection, disabled-first sequencing, assignment of user only, duplicate handling, missing/composite roles, failed assignment, creation ambiguity and activation reconciliation. The HTTP integration uses scripted responses so failure cases do not require changing the real realm.

The user-creation frontend remains the next stage. Existing catalog pages, user passwords and user roles are not changed by this implementation.

After the BFF migration, use Swagger through http://127.0.0.1:5173/swagger after signing in with an admin account. It sends the session cookie and fetches antiforgery tokens for writes. Bearer tokens alone no longer authenticate the BFF API. See bff-authentication.md.

## Implementation verification

The local cleanarq-user-provisioning client was created, client authentication/service-account roles enabled, and realm-management/manage-users granted after explicit approval. Its secret was saved to API User Secrets. No secret is committed. Existing ordinary and administrator accounts were not modified.

All 25 tests passed, including the opt-in real Keycloak test. The live test generated a unique disposable account, confirmed activation, user without admin, required password change and duplicate rejection, then deleted that specific test account. It does not test interactive password change or login for that disposable account.

To opt into the live test in PowerShell (it creates and removes a disposable account):

    $env:CLEANARQ_RUN_KEYCLOAK_SMOKE = '1'
    dotnet test Back/CleanArq.Api.Tests/CleanArq.Api.Tests.csproj --artifacts-path .artifacts
    Remove-Item Env:CLEANARQ_RUN_KEYCLOAK_SMOKE

Without this flag, 24 tests run and the live test is skipped. Verification used an isolated artifacts directory because an existing API process was running. Restart your API terminal to load the new controller, configuration and service secret. No API/frontend background process was started by this implementation.

A subsequent verification after stopping the previous API instance passed all 25 tests using the normal build output. A freshly started API served Swagger successfully (200), documented POST /api/users with CreateUserRequest and the Bearer scheme, and rejected unauthenticated POST /api/users and GET /api/Products with 401. The temporary API process was stopped after verification.

