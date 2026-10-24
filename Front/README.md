# CleanArq frontend

React, TypeScript, Tailwind CSS and Axios inventory workspace. All interface text is in English.

## Run locally

Start the API in a separate terminal:

    dotnet run --project Back/CleanArq.Api --launch-profile http

In Front:

    npm install
    npm run dev

Open http://127.0.0.1:5173/. The development proxy forwards /api to http://localhost:5012. Copy .env.example to .env to override configuration. MySQL and the API must be running.

## Simulated authentication

Enter any nonempty username and password in development. No credentials are stored or transmitted, and the session lives only in memory. Do not use real passwords. VITE_ENABLE_DEMO_LOGIN=false disables demo entry. Production builds do not permit simulated sign-in. Keycloak integration is deferred to the next stage.

## Catalog

Products and categories consume the real API, with create, read, update and delete operations, category selection, search, validation, loading and error states, duplicate-submit prevention, and deletion confirmation. Frontend limits follow the intersection of DTO and domain rules. Backend validation remains authoritative. The existing categories endpoint's exact empty-list 404 is treated as an empty catalog.

## Build and verification

    npm run build

TypeScript checking and production build passed. Product and category create, edit and delete flows were verified against the real API and database. Temporary test records were removed.

On Windows the Vite runner creates a temporary unused drive alias to work around # in the C# folder name, without moving the project. It removes the alias on normal exit.
