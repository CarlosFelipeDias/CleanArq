# BFF authentication

## Architecture

The .NET API now also acts as an integrated Backend for Frontend. The browser calls this backend with a session cookie; it no longer receives OAuth access/refresh tokens or calls Keycloak's token endpoint. Domain/Application services remain the same. The user-provisioning service account remains independent.

React navigates to GET /bff/login. ASP.NET Core OpenIdConnect performs Authorization Code + PKCE as confidential client cleanarq-backend. Keycloak hosts the themed password form. The callback is /signin-oidc. The server authenticates with its own client secret, validates the ID token and access token, checks their subjects match, and maps realm roles from the access token.

SaveTokens is enabled ONLY together with ServerTicketStore (ITicketStore). The full authentication ticket, including tokens, is serialized into server memory. The browser receives an encrypted/signed opaque session reference in CleanArq.Session, not a ticket containing OAuth tokens. Session responses expose only authentication state, username, roles and a CSRF request token.

## Endpoints

- GET /bff/session: no-store session state and identity-bound antiforgery request token.
- GET /bff/login: initiates OIDC login. No arbitrary return URL is accepted.
- POST /bff/logout: requires session and antiforgery; removes the local server ticket and initiates Keycloak logout.
- Existing /api routes: cookie authentication; product/category policy permits admin or user, user creation requires admin.

Bearer tokens alone no longer authenticate these BFF routes. API responses remain 401/403 instead of redirecting API requests to HTML login pages. Login redirects are initiated only through the explicit BFF login endpoint.

## CSRF and cookies

All unsafe MVC actions use AutoValidateAntiforgeryToken. This includes product/category writes, user creation and logout. GET/HEAD/OPTIONS do not require the header. /bff/session returns a request token that React sends in X-CSRF-TOKEN for writes; the antiforgery cookie is HttpOnly and SameSite=Strict. The request token is not an OAuth token or authentication credential by itself.

The session cookie is HttpOnly, SameSite=Lax, host-only, with no Domain wildcard. Production cookies are Secure; the development-only SameAsRequest exception permits the existing local HTTP workflow. Correlation and nonce cookies use Lax with GET/query OIDC callbacks. Outside Development, both configured authority and public origin must be HTTPS.

React uses same-origin relative API URLs. Vite proxies /api, /bff, /signin-oidc, /signout-callback-oidc and /swagger to the backend. Production hosting must route those paths to .NET and serve React under the same public origin. Do not add wildcard credentialed CORS.

## Session lifetime and refresh

Sessions expire after 30 minutes without successful token renewal, with an eight-hour absolute maximum. Before the access token expires, the backend exchanges the refresh token with Keycloak and validates the new token's signature, issuer, audience, expiration and subject. Roles are replaced from the refreshed token. A per-session lock prevents concurrent use of the same rotating refresh token; the renewed ticket is written before releasing the lock.

Refresh failure invalidates the local session; the frontend returns to sign-in. Roles are snapshots until renewal; Keycloak role/session changes do not instantaneously revoke a previously issued token/session. Logout removes the server ticket so the old local cookie cannot be reused.

The in-memory ticket store is appropriate for the current single-instance development application. Restarting the backend signs everyone out. Production with multiple replicas requires a distributed ITicketStore, shared persistent Data Protection keys and deployment-specific session/revocation decisions. Do not simply remove SessionStore: that would put saved tokens into the browser's protected cookie.

## Keycloak configuration

Client cleanarq-backend becomes the confidential interactive login client: client authentication on, Standard Flow on, PKCE S256 required. Direct access grants, implicit flow and service accounts stay off. The client has no administrative role.

Exact local callback: http://127.0.0.1:5173/signin-oidc.
Exact local post-logout callback: http://127.0.0.1:5173/signout-callback-oidc.
A dedicated Audience mapper includes cleanarq-backend in access-token aud (not ID-token aud). ID-token audience is validated separately by the OIDC handler against ClientId.

The former public cleanarq-frontend client is no longer used by React. It may be disabled after successful verification, without deleting the client or changing users. The cleanarq-user-provisioning client and its manage-users role remain separate.

## Configuration and execution

Bff:PublicOrigin=http://127.0.0.1:5173 and Keycloak:Bff:ClientId=cleanarq-backend are non-secret configuration. The new client secret belongs in API User Secrets as Keycloak:Bff:ClientSecret, separate from Keycloak:Provisioning:ClientSecret. Never put either secret into VITE_ variables.

In separate terminals:

    dotnet run --project Back/CleanArq.Api --launch-profile http

    cd Front
    npm run dev

Open http://127.0.0.1:5173/. Restart existing API/frontend processes to load the migration. The frontend no longer depends on keycloak-js. VITE_KEYCLOAK_* values are no longer used. Direct catalog API calls need a BFF session rather than a pasted JWT.

## Swagger

Open http://127.0.0.1:5173/swagger through the same origin after signing in. Swagger uses the browser session cookie and obtains X-CSRF-TOKEN from /bff/session for unsafe requests. The former Bearer Authorize configuration was removed. No token is shown or copied into Swagger. Swagger remains development-only.

## Verification

Tests use an in-memory HTTP host with real cookie middleware, ticket storage, authorization, antiforgery and scripted refresh responses. Test-only sign-in helpers are not present in production routes. Cases cover opaque cookie content, session JSON without OAuth tokens, session removal/expiry, role checks, ignored Bearer input, rejected missing/invalid antiforgery, valid writes and logout, refreshed roles, invalid refreshed tokens and refresh failure. Existing provisioning tests remain in place.

    dotnet test Back/CleanArq.Api.Tests/CleanArq.Api.Tests.csproj --artifacts-path .artifacts

The real Keycloak provisioning test is still opt-in; it creates/deletes a disposable account. Interactive BFF login/logout must also be checked with the local Keycloak and an existing user's credentials.

## Recorded verification

The frontend production build passed. All 33 backend tests passed with the real provisioning test enabled. The live HTTP checks returned session 200 with authenticated=false and no access/refresh tokens, catalog 401 without a session cookie, and Swagger 200 with cookie security metadata. The browser reached the themed Keycloak login as cleanarq-backend using the .NET confidential OIDC flow (including the provider-supported pushed authorization request). The BFF client secret was stored in API User Secrets; neither secret was exposed to React or committed. Interactive BFF login was subsequently verified using the authorized existing user account: the real catalog loaded, a disposable category was created and deleted with antiforgery protection, page reload restored the session, and logout returned to the sign-in view. The three existing products and three existing categories were preserved. The old cleanarq-frontend client was then disabled (not deleted). Temporary API/frontend test processes were stopped.

