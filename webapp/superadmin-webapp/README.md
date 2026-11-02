# Superadmin Console

Dedicated workspace for platform superadmins to create tenants, inspect tenant configuration, and pause or resume a workspace.

## Run locally

From this app's directory, run:

```sh
npm install
npm run dev
```

Vite serves the console on `http://localhost:3004`.

## Current state

This is a UI preview. It starts with sample tenants and saves changes to this browser's local storage. It is not connected to an identity provider or tenant API, so it must not be used to provision real customer data.

When the platform API is ready, connect tenant reads and writes to authenticated superadmin endpoints. The API must validate the superadmin role on every request; hiding UI controls in the browser is not an authorization boundary.
