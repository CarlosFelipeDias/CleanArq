# CleanArq frontend

React, TypeScript, Tailwind CSS and Axios inventory workspace. All interface text is in English.

## Development

Run the API in a separate terminal:

    dotnet run --project Back/CleanArq.Api --launch-profile http

In Front:

    npm install
    npm run dev

Open http://127.0.0.1:5173/. Sign in uses the CleanArq realm through Keycloak. The former simulated login has been removed. Existing admin and user roles can access the catalog.

Copy .env.example to .env to override the public configuration. No client secret belongs in the frontend. Axios attaches a refreshed access token; tokens are not persisted in browser storage by application code.

The Vite development proxy forwards /api to http://localhost:5012. Production hosting must provide an equivalent reverse proxy or explicitly configured backend CORS. Production also requires HTTPS Keycloak and frontend addresses registered in the realm.

## Build

    npm run build

The Vite runner uses a temporary free drive alias on Windows to work around # in the C# folder name, preserving the actual project location. It removes its alias on normal process exit.

## API contracts

Forms follow the intersection of existing DTO and domain validation limits: name 3–100 characters, product description 10–255, positive price, stock 1–10000 and optional image URL up to 220 characters. Backend validation remains authoritative. Categories returning the exact empty-list 404 are displayed as an empty catalog; other failures remain visible.

See ../Keycloak/README.md for authentication configuration, request flow, theme installation and test instructions.
