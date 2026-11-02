# Frontend apps

Each folder here is an independent Vite/React application with its own `package.json`, lockfile, source code, and build configuration. There is no shared frontend workspace.

## Run an app locally

Install dependencies and start the development server from the app's folder. For example:

```sh
cd webapp/dashboard-webapp
npm install
npm run dev
```

Replace `dashboard-webapp` with `erp-webapp`, `hrms-webapp`, or `superadmin-webapp` to run another app. Each app also provides `npm run build`, `npm run lint`, and `npm run preview`.

The current pages use sample data. The superadmin console saves its preview tenant changes in browser local storage. Connect each app to its backend API independently; server-side authorization remains required for tenant and employee operations.
