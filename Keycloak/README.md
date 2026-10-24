# Local Keycloak configuration

Keycloak 26.0 runs in keycloak_local at http://localhost:8180 with realm CleanArq. Realm names are case-sensitive.

## Current clients

- cleanarq-backend: confidential BFF login client, Authorization Code with PKCE S256. Its secret is Keycloak:Bff:ClientSecret in API User Secrets. Redirect: http://127.0.0.1:5173/signin-oidc. Post-logout callback: http://127.0.0.1:5173/signout-callback-oidc. No service account or admin-management role.
- cleanarq-user-provisioning: separate confidential service-account client for the API's user-creation integration. Its secret is Keycloak:Provisioning:ClientSecret. The approved realm-management/manage-users permission is broader than create-only; the API restricts its use to creating ordinary users.
- cleanarq-frontend: the previous public React client, no longer referenced by frontend code. Disabled after successful BFF login, write, reload and logout validation; retained without deletion for rollback.

The backend client's dedicated Audience mapper adds cleanarq-backend to access-token aud. ASP.NET validates the access token before using its realm roles. Users retain their existing admin/user roles. Neither client secret belongs in React environment variables.

## Authentication and provisioning

See ../docs/bff-authentication.md for the current server-side session flow, cookies, CSRF, token renewal and deployment limitations. See ../docs/user-provisioning.md for disabled-first creation, partial failures and administrative permissions.

The previous React Bearer integration was replaced by an integrated BFF in the .NET project. The browser no longer obtains access or refresh tokens. It receives a protected reference to a server-side session. Restarting the current single-instance memory store invalidates sessions.

## Theme

The cleanarq login theme inherits Keycloak's built-in forms and validation and adds the catalog's colors and typography. Install-Theme.ps1 copies themes/cleanarq to /opt/keycloak/themes/cleanarq. Select cleanarq under Realm settings > Themes > Login theme. No admin-console or other-realm theme is changed.

The existing container mounts its data directory but not themes. Restart preserves the copied theme; recreating the container does not. Reinstall the theme after container recreation or mount the repository's theme directory in the existing compose setup. Preserve the data volume.

## Run and test

Run the backend and frontend in separate terminals. Open http://127.0.0.1:5173/ (the exact registered origin). Use /swagger through that same frontend origin after logging in. Do not paste JWTs into Swagger for the BFF.

    dotnet run --project Back/CleanArq.Api --launch-profile http

    cd Front
    npm run dev

Tests:

    dotnet test Back/CleanArq.Api.Tests/CleanArq.Api.Tests.csproj --artifacts-path .artifacts

Set CLEANARQ_RUN_KEYCLOAK_SMOKE=1 only to opt into the live provisioning test, which creates and deletes its own disposable account. Existing users and credentials are not modified by tests. Existing dependency vulnerability advisories remain a separate dependency-maintenance task.

