# CleanArq frontend

React, TypeScript, Tailwind and Axios inventory workspace. Interface text is in English.

Authentication uses the .NET BFF and a server-side session. No OAuth tokens or client secrets are stored by React. Sign-in navigates to /bff/login and the themed Keycloak form. API writes include an antiforgery request token; logout is a protected navigation POST.

Start the API in a separate terminal, then run the frontend:

    dotnet run --project Back/CleanArq.Api --launch-profile http

In Front:

    npm install
    npm run dev

Open http://127.0.0.1:5173/. The Vite development proxy forwards API and BFF callback paths to http://localhost:5012. Copy .env.example to .env to change the proxy target; API URLs must remain same-origin relative paths. Production hosting must provide equivalent routing under HTTPS.

    npm run build

The Vite runner uses a temporary drive alias on Windows to work around # in the C# folder name, without moving the project. Restarting .NET invalidates the current in-memory session store.

Catalog forms use the existing API contracts and validation limits. User provisioning remains backend-only until its frontend screen is implemented.

See ../docs/bff-authentication.md for configuration, session security, deployment limitations and tests, and ../docs/user-provisioning.md for user creation.
