# CleanArq

Study project focused on Clean Architecture, ASP.NET Core, Entity Framework Core, and good separation of concerns practices.

## Goal

This repository is being built as a layered API, following the idea of keeping business rules isolated from external infrastructure.

The current structure separates:

- `CleanArq.Domain`: domain entities, interfaces, and core business rules.
- `CleanArq.Application`: application layer, intended for use cases and application services.
- `CleanArq.Infra.Data`: persistence layer using Entity Framework Core and MySQL.
- `CleanArq.Infra.IoC`: dependency injection registration.
- `CleanArq.Api`: executable API project, where `Program.cs`, configuration, and Swagger are located.
- `CleanArq.Domain.Tests`: domain tests.

## Technologies

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- MySQL
- Swagger / OpenAPI
- dotnet user-secrets for local development secrets

## Configuration

Secrets must not be committed to the repository. Connection strings, client secrets, passwords, and tokens must stay outside `appsettings.json`.

For local development, use `dotnet user-secrets` from the executable API project:

```powershell
cd Back\CleanArq.Api
```

Set the Docker connection string:

```powershell
dotnet user-secrets set "ConnectionStrings:DockerConnection_Dev" "Server=127.0.0.1;Port=3319;Uid=devuser;Pwd=your_password;Charset=utf8;Pooling=true;Connection Timeout=120;"
```

Set the Keycloak client secret when authentication is added:

```powershell
dotnet user-secrets set "Keycloak:ClientSecret" "your_keycloak_client_secret"
```

To verify the configured secrets:

```powershell
dotnet user-secrets list
```

## Database

The sensitive connection string is stored in `user-secrets`, without the database name.

The temporary database name is stored in `Back/CleanArq.Api/appsettings.json`:

```json
{
  "Database": {
    "Name": "CleanArchDB01"
  }
}
```

Currently, the IoC layer builds the final connection string using:

- `ConnectionStrings:DockerConnection_Dev`: server, port, user, password, and connection options.
- `Database:Name`: temporary database name.

This separation was chosen to make a future multi-store scenario easier, where each store may have its own database resolved from the authenticated user context.

## Environments

In development:

- Use `dotnet user-secrets`.
- Do not store passwords, tokens, or client secrets in `appsettings.json`.

In testing, staging, and production:

- Use environment variables or the infrastructure secret manager.
- The application should keep reading the same keys through `IConfiguration`.

Environment variable example:

```powershell
ConnectionStrings__DockerConnection_Dev="Server=...;Port=...;Uid=...;Pwd=...;Charset=utf8;Pooling=true;Connection Timeout=120;"
Keycloak__ClientSecret="..."
```

In ASP.NET Core, `__` in environment variables maps to `:` in configuration keys.

## Running

Restore and build:

```powershell
dotnet build Back\CleanArq.Api\CleanArq.Api.csproj
```

Run the API:

```powershell
dotnet run --project Back\CleanArq.Api\CleanArq.Api.csproj
```

In development, the application root redirects to Swagger.

## Notes

The project is still evolving. Database creation and the final migration strategy should be adjusted as the implementation progresses.

In the future, connection string resolution may include a local fallback and/or dynamic database selection per authenticated store/tenant.
