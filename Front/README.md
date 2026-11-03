# CleanArq frontend

React, TypeScript, Tailwind and Axios inventory workspace. Interface text is in English.

For complete project setup, including Keycloak, database configuration and API-only testing, see the [main project README](../README.md).

Authentication uses the .NET BFF and a server-side session. No OAuth tokens or client secrets are stored by React. Sign-in navigates to /bff/login and the themed Keycloak form. API writes include an antiforgery request token; logout is a protected navigation POST.

Start the API in a separate terminal, then run the frontend:

    dotnet run --project Back/CleanArq.Api --launch-profile http

In Front:

    npm install
    npm run dev

Open http://127.0.0.1:5173/. The Vite development proxy forwards API and BFF callback paths to http://localhost:5012. Copy .env.example to .env to change the proxy target; API URLs must remain same-origin relative paths. Production hosting must provide equivalent routing under HTTPS.

    npm run build

The Vite runner uses a temporary drive alias on Windows to work around # in the C# folder name, without moving the project. Restarting .NET invalidates the current in-memory session store.

Catalog forms use the existing API contracts and validation limits. Administrators also see a Users section for creating ordinary users through POST /api/users. The form collects first name, last name, email and a confirmed temporary password (8–128 characters); the API always assigns the user role. New accounts must change their password at first sign-in. Passwords are cleared after each request and are never saved in browser storage.

The Users section is hidden for ordinary users; the API independently enforces the admin role and antiforgery validation. After a partial failure, server error or lost response, the form asks the administrator to review the email/account in Keycloak before starting a new request. It does not automatically retry account creation. This screen creates accounts only; user listing, editing and deletion are managed in Keycloak.

See ../docs/bff-authentication.md for configuration, session security, deployment limitations and tests, and ../docs/user-provisioning.md for user creation.
