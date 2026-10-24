# Keycloak authentication integration

## Responsibilities

Keycloak authenticates users, manages credentials and sessions, and signs tokens. React is a public OpenID Connect client. The ASP.NET API is a resource server: it validates access tokens and applies authorization. No API login endpoint or password database was introduced.

## Local configuration

- Keycloak version observed: 26.0, container keycloak_local, port 8180.
- Realm: CleanArq (case-sensitive).
- Frontend: http://127.0.0.1:5173/.
- API: http://localhost:5012.
- Issuer/authority: http://localhost:8180/realms/CleanArq.
- Expected API audience: cleanarq-backend.

Use the exact frontend address above. localhost and 127.0.0.1 are distinct origins. Another origin must be explicitly registered before it can be used.

### cleanarq-frontend

Public OpenID Connect client; client authentication disabled. Standard flow enabled. Direct grants, implicit flow and service accounts disabled. Redirect and post-logout redirect are exactly http://127.0.0.1:5173/. Web origin is http://127.0.0.1:5173. PKCE S256 is required under Advanced settings.

The dedicated scope contains an Audience mapper named cleanarq-backend-audience. Included Custom Audience is cleanarq-backend; Add to access token is enabled and Add to ID token is disabled. This puts the API identifier in aud without granting management privileges.

### cleanarq-backend

Represents the token recipient. All interactive and direct grant flows are disabled, and service accounts are disabled. The API validates JWTs using the realm's public keys; no client secret is needed for this operation. This client does not create users or receive administrative permissions.

### Users

The two existing users were inspected. The first has admin and the second user. The second also has a required Update Password action, which was preserved. No credentials, user roles, public registration settings or existing user data were changed.

## Request flow

1. AuthGate initializes keycloak-js once, including under React StrictMode. check-sso restores an existing realm session through a redirect when needed.
2. An unauthenticated visitor sees the branded landing page. Sign in navigates to the Keycloak login page.
3. The Keycloak-hosted form receives credentials. React and the API do not receive the password.
4. Keycloak returns a temporary authorization code to the registered redirect URI. The adapter validates the flow and exchanges the code using its PKCE verifier.
5. Tokens remain in adapter memory, without localStorage/sessionStorage persistence. PKCE callback metadata may be temporarily handled by the adapter in browser storage.
6. Axios calls updateToken(30) before API requests, then attaches the access token as Authorization: Bearer. The ID token is not used to access the API.
7. JWT Bearer middleware retrieves OpenID metadata/public keys and validates signature, issuer, audience and lifetime. Normal JWT validation does not require a Keycloak request for every API call.
8. Realm roles from realm_access.roles become individual ASP.NET role claims. CatalogAccess permits admin OR user.
9. A 401 or failed renewal clears local authentication state and returns to the sign-in view. A 403 is shown as an access-denied message.
10. Sign out ends the Keycloak session and redirects back to the frontend.

Access tokens already issued may remain usable until expiration even after logout or a role change. Offline JWT validation is not immediate session revocation. New tokens reflect updated roles.

## Backend files

Security/KeycloakAuthentication.cs registers JWT validation and authorization policies. MapInboundClaims is false so original OIDC claim names remain predictable. preferred_username is the identity name. Clock skew tolerance is 30 seconds. RequireHttpsMetadata is disabled only in Development; production requires an HTTPS authority.

Program.cs places UseAuthentication before UseAuthorization. ProductsController and CategoriesController explicitly require CatalogAccess. A fallback policy requires authentication on other controller endpoints. Swagger and OpenAPI are available anonymously only under the existing Development condition; the catalog itself remains protected.

Keycloak settings are in appsettings.Development.json. Production must explicitly provide Keycloak__Authority and Keycloak__Audience. No signing secret is stored by this API.

## Theme

Keycloak/themes/cleanarq/login inherits the built-in keycloak theme and adds CSS. Native templates and form validation, password visibility, errors, required actions and accessibility behavior are retained. Colors, typography and panel layout match the inventory frontend. Locale is English.

Install-Theme.ps1 copies the theme into /opt/keycloak/themes/cleanarq. The theme was installed and the container restarted to refresh theme discovery. The CleanArq realm's Login theme was selected as cleanarq. Other realms and the admin-console theme were not changed.

The current container persists /opt/keycloak/data but does not mount /opt/keycloak/themes. A restart retains the copied theme; recreating the container does not. Re-run the installation script after recreation, or add a read-only theme volume to your existing compose configuration. Do not replace or remove the existing data volume.

## Run

In Back/CleanArq.Api:

    dotnet run --launch-profile http

In Front:

    npm install
    npm run dev

Open http://127.0.0.1:5173/. Keycloak and MySQL must be running. Do not run duplicate instances on ports 5012/5173. Stop each instance with Ctrl+C.

## Automated tests

    dotnet test Back/CleanArq.Api.Tests/CleanArq.Api.Tests.csproj

The tests use the actual authentication configuration and policy in an in-memory HTTP server, with temporary RSA-signed test tokens and static test public-key metadata. No real credentials or production keys are used. Cases: admin and user accepted (200); missing token, wrong audience, wrong issuer, expired token and wrong signature rejected (401); absent or unrelated role rejected (403). These tests do not claim to exercise the real Keycloak password flow.

## Manual verification

- Sign in with each existing account and confirm catalog data loads.
- Complete the required password change yourself if prompted; never share the password in chat.
- Reload the frontend and confirm the SSO session restores access.
- Sign out and confirm the catalog is no longer shown.
- Call /api/Products or /api/Categories without a token: expect 401.
- An authenticated account without admin/user should receive 403 from catalog endpoints.
- Test both GET and write operations after login using disposable records.
- Check the login layout at a narrow viewport and verify invalid credentials remain on the Keycloak form with a native error.

## Deferred scope

User provisioning, administrative service credentials, admin-only creation endpoints and the user-creation screen remain future work. Both roles currently have catalog CRUD access. Existing dependency vulnerability advisories require a separate dependency review; they were not suppressed or upgraded as part of authentication.

## Verification recorded for this change

- API build and frontend TypeScript/production build passed.
- All 9 authentication integration cases passed.
- Live Products and Categories GET requests without a token returned 401.
- The browser followed Authorization Code + PKCE S256 to the custom themed login form.
- The existing user account's temporary credential was accepted and the native required password-update screen was reached. Password update must be completed by the user; authenticated catalog access, session restoration and logout remain pending manual confirmation.
- No passwords or access/refresh tokens were written into project files.
