# CleanArq: project goals and local setup

This guide explains the current project, its Keycloak configuration, and the steps required to run the backend and frontend locally. Commands use PowerShell on Windows.

## 1. Project goals

CleanArq is a learning project for building a complete application with clear responsibilities across layers. Its current business functionality is product and category management: listing, creating, editing, and deleting records stored in MySQL.

The project is used to practice:

- C#/.NET, HTTP APIs, validation, persistence, and automated tests.
- Clean Architecture: domain rules separated from application use cases and external infrastructure.
- A component-based React/TypeScript frontend using Tailwind CSS and Axios.
- Authentication and role-based authorization with Keycloak.
- A Backend for Frontend (BFF), with OAuth tokens kept on the .NET server.
- Administrative user provisioning with explicit handling of partial failures.

This is an evolving local-development project, not a production deployment template. The user-creation endpoint is implemented; its frontend screen is still a later step.

### Repository structure

| Location | Responsibility |
| --- | --- |
| `Back/CleanArq.Domain` | Product/category entities and business rules |
| `Back/CleanArq.Application` | Application services, use cases, DTOs, and provisioning contract |
| `Back/CleanArq.Infra.Data` | EF Core/MySQL persistence and migrations |
| `Back/CleanArq.Infra.Identity` | Keycloak administrative HTTP integration |
| `Back/CleanArq.Infra.IoC` | Dependency injection and configuration |
| `Back/CleanArq.Api` | HTTP endpoints, BFF login/session, authorization, and antiforgery |
| `Back/CleanArq.Domain.Tests` | Domain tests |
| `Back/CleanArq.Api.Tests` | Authentication, session, antiforgery, and provisioning tests |
| `Front` | React application |
| `Keycloak/themes/cleanarq` | Custom login theme |

Users are currently stored in Keycloak, not in a local MySQL user table. There is no Domain `User` entity because no application-specific user business data is required yet.

## 2. Prerequisites and local addresses

Install the .NET 10 SDK, Node.js with npm, and Docker Desktop with Linux containers enabled. Node.js 22.17.1 was used during implementation; use a Node version compatible with the Vite version in `Front/package-lock.json`. Database migrations also require the `dotnet-ef` tool matching the project's EF Core major version (10).

The current local environment uses:

| Component | Address or value |
| --- | --- |
| Frontend and BFF public origin | `http://127.0.0.1:5173` |
| Backend HTTP listener | `http://localhost:5012` |
| Swagger through the frontend proxy | `http://127.0.0.1:5173/swagger` |
| Keycloak admin console | `http://localhost:8180/admin/master/console/` |
| Keycloak issuer | `http://localhost:8180/realms/CleanArq` |
| MySQL host/port | `127.0.0.1:3319` |
| Application database | `CleanArchDB01` |
| Existing Docker containers | `keycloak_local` and `mysql_local` |

The existing containers use Keycloak 26.0 and MySQL 8.0. No Docker Compose file is currently checked into this repository. If the containers do not exist on your machine, provision those services first, with persistent storage, the ports above, and a MySQL account allowed to manage the application database. Use your own administrative credentials.

All commands below start from the repository root unless another directory is explicitly shown:

```powershell
Set-Location -LiteralPath 'D:\Estudo_cursos\C#\project_vs_code\CleanArq'
```

Use **`127.0.0.1:5173`**, not `localhost:5173`, for the browser. They are different origins; only the former is registered for the current BFF callbacks.

## 3. Start the supporting services

For the existing local containers:

```powershell
docker start mysql_local keycloak_local
docker ps
```

Wait until MySQL accepts connections and the Keycloak admin console opens. Starting these containers does not start the .NET API or React application.

For an already configured environment, skip to section 5. Follow section 4 when creating or checking the realm on a new installation.

## 4. Configure the Keycloak realm

Sign in to the admin console using your Keycloak administrator account. Realm names are case-sensitive.

### 4.1 Realm, roles, and application users

1. Create/select the realm **`CleanArq`**. Do not configure the application inside `master` or `syscourse`.
2. Under **Realm roles**, create the non-composite roles **`admin`** and **`user`**.
3. Under **Realm settings > Login**, keep **User registration** disabled.
4. Create the application's initial administrator under **Users**.
5. In that user's **Role mapping > Assign role**, choose **Filter by realm roles** and assign **`admin`**.
6. Set the user's password under **Credentials**. If it is temporary, the user must change it at first login.
7. Optionally create an ordinary account and assign **`user`** for permission testing.

An application user with role `admin` is not automatically an administrator of the Keycloak console. Keeping only one application administrator is a manual administration convention; the role itself does not impose a one-user limit. Do not include `admin` in realm-default roles.

Both application roles can use product/category CRUD. Only `admin` can call the user-creation endpoint. Ordinary users can also be created manually in the Keycloak console.

### 4.2 BFF login client: `cleanarq-backend`

Create an **OpenID Connect** client with the following settings:

| Setting | Value |
| --- | --- |
| Client ID | `cleanarq-backend` |
| Enabled | On |
| Client authentication | On |
| Standard flow | On |
| Direct access grants | Off |
| Implicit flow | Off |
| Service accounts roles | Off |
| Authorization | Off |
| Device Authorization / CIBA | Off |
| Root URL | `http://127.0.0.1:5173` |
| Home URL | `http://127.0.0.1:5173/` |
| Valid redirect URIs | `http://127.0.0.1:5173/signin-oidc` |
| Valid post logout redirect URIs | `http://127.0.0.1:5173/signout-callback-oidc` |
| Web origins | Leave empty; the browser does not call Keycloak's token endpoint |

Under **Advanced**, set **Proof Key for Code Exchange Code Challenge Method** to **`S256`** and save.

Under **Client scopes**, open **`cleanarq-backend-dedicated`**, choose **Configure a new mapper > Audience**, and set:

- Name: `cleanarq-bff-audience`.
- Included Custom Audience: `cleanarq-backend`.
- Add to access token: On.
- Add to ID token: Off.

The normal `roles` client scope must remain assigned as a **Default** scope so that realm roles are included in access tokens. The API validates access-token audience separately from the OIDC ID-token audience.

In **Credentials**, obtain this client's secret for the User Secrets command in section 5. No administrative role is granted to this login client.

### 4.3 Administrative client: `cleanarq-user-provisioning`

Create a separate **OpenID Connect** client:

| Setting | Value |
| --- | --- |
| Client ID | `cleanarq-user-provisioning` |
| Client authentication | On |
| Service accounts roles | On |
| Standard flow / Direct access grants / Implicit flow | Off |
| Authorization / Device Authorization / CIBA | Off |
| Interactive redirect URLs | Not required |

Under **Service accounts roles > Assign role**, use **Filter by clients**, search for `manage-users`, and assign only **`realm-management / manage-users`** as an administrative role. Do not grant `realm-admin` or master-realm permissions.

This Keycloak permission can manage existing realm users as well as create new ones. The application's endpoint enforces the narrower rule: an authenticated `admin` may create only ordinary `user` accounts. Fine-grained administrative delegation is a separate hardening task.

Obtain this client's own secret under **Credentials**. It is different from the BFF login client's secret.

### 4.4 Previous frontend client

**`cleanarq-frontend` is disabled and no longer used.** It belonged to the previous React/Bearer implementation. A fresh BFF installation does not need to create it. Do not enable it or place a client secret in React to make the current login work.

### 4.5 Install the login theme

From the repository root:

```powershell
.\Keycloak\Install-Theme.ps1 -ContainerName keycloak_local
```

If the new theme is not listed, restart Keycloak and reload the console:

```powershell
docker restart keycloak_local
```

Select **Realm settings > Themes > Login theme > `cleanarq`** and save. This changes the CleanArq login pages, not the admin-console theme or other realms.

The current container persists `/opt/keycloak/data` but does not mount `/opt/keycloak/themes`. Restarting keeps the copied theme; recreating the container does not. Reinstall after recreation, or mount the repository's theme directory into the existing Docker setup. Preserve the data volume containing the realms and users.

## 5. Configure .NET User Secrets

The executable API project already declares `UserSecretsId=cleanarq-api-dev-secrets`; there is no need to run `dotnet user-secrets init` again.

Run these commands from the repository root. Replace every angle-bracket placeholder with your own value; the examples contain no real credentials.

```powershell
dotnet user-secrets set "ConnectionStrings:DockerConnection_Dev" "Server=127.0.0.1;Port=3319;Uid=<mysql-user>;Pwd=<mysql-password>;Charset=utf8;Pooling=true;Connection Timeout=120;" --project Back/CleanArq.Api

dotnet user-secrets set "Keycloak:Bff:ClientSecret" "<cleanarq-backend-client-secret>" --project Back/CleanArq.Api

dotnet user-secrets set "Keycloak:Provisioning:ClientSecret" "<cleanarq-user-provisioning-client-secret>" --project Back/CleanArq.Api
```

These three settings have different purposes:

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:DockerConnection_Dev` | Connect the API to MySQL |
| `Keycloak:Bff:ClientSecret` | Authenticate the .NET BFF during login/code exchange and token renewal |
| `Keycloak:Provisioning:ClientSecret` | Obtain the administrative service token for user provisioning |

The API adds the database name from `Database:Name` (`CleanArchDB01`) to the connection string. It is configured separately in `Back/CleanArq.Api/appsettings.json`.

Use the **exact keys above**, not the older generic `Keycloak:ClientSecret` key. Run the same `set` command again to update an existing value. When regenerating a secret in Keycloak, update only its corresponding key and restart the backend.

To inspect your local configuration:

```powershell
dotnet user-secrets list --project Back/CleanArq.Api
```

That command prints secret values: run it privately and do not share its output. User Secrets keeps development settings outside the repository; it is not an encrypted production vault. Command-line values may also remain in your terminal history. Never commit passwords, tokens, or secrets, and never put client secrets in `VITE_` variables.

The non-secret development settings are already in `Back/CleanArq.Api/appsettings.Development.json`:

```json
{
  "Keycloak": {
    "Authority": "http://localhost:8180/realms/CleanArq",
    "Audience": "cleanarq-backend",
    "Bff": {
      "ClientId": "cleanarq-backend"
    },
    "Provisioning": {
      "ServerUrl": "http://localhost:8180",
      "Realm": "CleanArq",
      "ClientId": "cleanarq-user-provisioning",
      "AllowHttpForDevelopment": true
    }
  },
  "Bff": {
    "PublicOrigin": "http://127.0.0.1:5173"
  }
}
```

## 6. Restore dependencies and initialize a new database

```powershell
dotnet restore Back/CleanArq.slnx

Push-Location Front
npm ci
Pop-Location
```

For an existing populated database, normally skip the initial setup below. For a new database, apply the checked-in EF Core migrations; do not generate a new initial migration.

Check that the EF tool is installed:

```powershell
dotnet ef --version
```

If it is missing, install a version matching the project's current EF Core version:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.1
```

**Important:** the design-time `ApplicationDbContextFactory` reads environment variables, not API User Secrets. Supply the database settings in the terminal that runs migrations:

```powershell
$env:ConnectionStrings__DockerConnection_Dev = "Server=127.0.0.1;Port=3319;Uid=<mysql-user>;Pwd=<mysql-password>;Charset=utf8;Pooling=true;Connection Timeout=120;"
$env:Database__Name = "CleanArchDB01"

try {
    dotnet ef database update --project Back/CleanArq.Infra.Data --startup-project Back/CleanArq.Api
}
finally {
    Remove-Item Env:ConnectionStrings__DockerConnection_Dev -ErrorAction SilentlyContinue
    Remove-Item Env:Database__Name -ErrorAction SilentlyContinue
}
```

These environment variables are temporary to that terminal. Removing them afterward also avoids unintentionally overriding User Secrets when the API is started from the same terminal. The migrations create the schema and include sample catalog data. The design-time fallback password in the factory is a placeholder, not a real database credential.

## 7. Run the backend and frontend

Open two terminals. Both examples below begin at the repository root.

**Terminal 1 — backend:**

```powershell
dotnet run --project Back/CleanArq.Api --launch-profile http
```

Wait for `Now listening on: http://localhost:5012`. The `http` profile sets the environment to Development, which loads the API User Secrets and local Keycloak settings.

**Terminal 2 — frontend:**

```powershell
Set-Location Front
npm run dev
```

Open [http://127.0.0.1:5173/](http://127.0.0.1:5173/). `Front/.env.example` contains the default proxy settings. If you need to override them, copy it once from inside `Front`:

```powershell
Copy-Item .env.example .env
```

Keep `VITE_API_BASE_URL=/api`. The development proxy forwards API/BFF paths to `http://localhost:5012`; the browser still uses the frontend origin. Restart Vite after changing `.env`.

Keep both terminals open. Press **Ctrl+C** in each terminal to stop its application. Do not start two API or frontend processes on the same port.

The Windows Vite runner creates a temporary unused drive alias to handle `#` in the `C#` folder name. The project stays in its original folder; no manual folder rename is required.

## 8. Use and verify the application

1. Choose **Sign in to CleanArq**. The .NET BFF redirects to the themed Keycloak login.
2. Enter an application user's email/password, not a client secret or the master administrator's credentials.
3. Complete any required password change directly in Keycloak.
4. Confirm that products and categories load and that create/edit/delete operations work.
5. Reload the page: the server-side session should restore access.
6. Choose **Sign out**: the local session is removed and the Keycloak logout flow returns to the sign-in view.

For Swagger, sign in first and open [http://127.0.0.1:5173/swagger](http://127.0.0.1:5173/swagger) through the same origin. Swagger uses the session cookie and obtains antiforgery tokens for writes. Do not paste a JWT into Swagger for this BFF implementation.

`POST /api/users` requires an `admin` session. It accepts email, first name, last name, and a temporary password; it does not accept a role selector. The account is created disabled, receives `user`, and is enabled only after successful provisioning. Unknown activation outcomes are reported for review, not presented as success. Ordinary users receive **403** for this endpoint.

Useful responses: **401** means no valid session, **403** means insufficient permission, **400** can indicate validation or missing/invalid antiforgery, and **409** indicates a duplicate user.

## 9. Build and test

From the repository root:

```powershell
dotnet build Back/CleanArq.slnx

dotnet test Back/CleanArq.Domain.Tests/CleanArq.Domain.Tests.csproj

dotnet test Back/CleanArq.Api.Tests/CleanArq.Api.Tests.csproj --artifacts-path .artifacts

Push-Location Front
npm run build
Pop-Location
```

The API tests cover sessions, role checks, antiforgery, token renewal, logout, and provisioning failures. The real-Keycloak provisioning test is skipped unless explicitly enabled. To run it against the configured local Keycloak:

```powershell
$env:CLEANARQ_RUN_KEYCLOAK_SMOKE = "1"
try {
    dotnet test Back/CleanArq.Api.Tests/CleanArq.Api.Tests.csproj --artifacts-path .artifacts
}
finally {
    Remove-Item Env:CLEANARQ_RUN_KEYCLOAK_SMOKE -ErrorAction SilentlyContinue
}
```

The live test creates a uniquely named disposable account, checks its configuration and duplicate handling, and removes it afterward. It requires the provisioning client secret in API User Secrets. It does not change existing application users.

## 10. Troubleshooting

| Symptom | Check |
| --- | --- |
| `Address already in use` | Stop the other API/frontend process with Ctrl+C; use ports 5012 and 5173 consistently |
| MySQL connection failure | Container is running; port, account/password, database name, and migrations are correct |
| Migration uses the wrong password | The design-time factory requires the temporary environment variables in section 6 |
| `invalid_client` or login cannot complete | BFF client authentication is enabled and its current secret matches `Keycloak:Bff:ClientSecret`; restart the API |
| Invalid redirect URI | Use `127.0.0.1:5173` and register the exact `/signin-oidc` and `/signout-callback-oidc` URLs |
| Login still uses `cleanarq-frontend` | Restart Vite and the API; use the current BFF frontend, not an old tab/build |
| Catalog returns 403 | Assign `admin` or `user` in realm role mappings, then sign in again |
| Write request returns 400 | Check validation messages and the antiforgery header/cookie; reload the page and sign in again if needed |
| User creation returns 503 | Check provisioning client authentication, service account, secret, and `manage-users` assignment |
| User creation reports incomplete/unknown state | Inspect that account in Keycloak before retrying; do not blindly repeat the POST |
| Theme disappears after container recreation | Reinstall or persist the theme as described in section 4.5 |
| File is locked during build | Stop the running API, or use isolated test output with `--artifacts-path .artifacts` |

Existing NuGet vulnerability advisories require dependency review; hiding the warnings does not resolve them.

## 11. Security and deployment limits

The browser receives an HttpOnly session cookie containing a protected reference. OAuth access/refresh tokens stay in the .NET server's ticket store. Unsafe actions require antiforgery validation. Keycloak claims/roles are validated server-side; hiding UI controls is not authorization.

The current ticket store is in memory and intended for one local API instance. Restarting it signs everyone out. Multiple production instances require a distributed ticket store and shared persistent Data Protection keys. Do not remove `SessionStore` while retaining `SaveTokens`, because that would move saved tokens into the browser's protected authentication ticket.

Local HTTP is a Development exception. Production requires HTTPS, Secure cookies, same-origin frontend/backend routing, explicit environment configuration, and an appropriate secret manager instead of development User Secrets. For example, configuration keys map to environment variables as follows:

- `Keycloak:Bff:ClientSecret` -> `Keycloak__Bff__ClientSecret`.
- `Keycloak:Provisioning:ClientSecret` -> `Keycloak__Provisioning__ClientSecret`.
- `Bff:PublicOrigin` -> `Bff__PublicOrigin`.

## Further reading in this repository

- [BFF authentication: flow, sessions, and security](../docs/bff-authentication.md)
- [User provisioning: endpoint and partial failures](../docs/user-provisioning.md)
- [Frontend instructions](../Front/README.md)
