# Nik Speed Properties LLC

A property marketplace for land, rentals, and homes for sale, operated by Nik Speed Properties LLC. Advertisers buy listing credits before publishing properties.

## Projects

- `backend/NikSpeed.Api` — ASP.NET Core Web API with SQLite persistence, JWT advertiser accounts, and Flutterwave checkout.
- `frontend` — Next.js 14 App Router frontend.

## Run locally

```powershell
cd backend/NikSpeed.Api
dotnet run
```

The API listens on `http://localhost:5188`. On its first run, it creates `nikspeed.db` and seeds example properties.

```powershell
cd frontend
npm install
npm run dev
```

Open `http://localhost:3000`. The frontend sends API requests through its same-origin `/api` proxy. When deploying the frontend and API separately, set `NEXT_PUBLIC_API_URL` at Next.js build time to the API origin (for example, `https://api.example.com`); the browser continues to call the frontend origin.

## Production configuration

Configure secrets outside source control. For local development, use .NET user secrets:

```powershell
cd backend/NikSpeed.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "a-long-unique-random-secret-at-least-32-characters"
dotnet user-secrets set "Flutterwave:SecretKey" "FLWSECK_TEST-..."
dotnet user-secrets set "Flutterwave:RedirectUrl" "http://localhost:3000/payment/complete"
```

The payment API creates a Flutterwave hosted checkout link when `Flutterwave:SecretKey` is set. Configure `Flutterwave:RedirectUrl` to the public frontend URL ending in `/payment/complete` and set `Flutterwave:WebhookHash` to the matching secret configured in Flutterwave. Register `POST /api/payments/flutterwave/webhook` as the webhook endpoint. Successful return redirects are independently verified against Flutterwave before marking a transaction paid; the signed webhook also confirms payments. Deploy the API behind HTTPS with a managed database (PostgreSQL or SQL Server is recommended over SQLite).

The default SQLite database and JWT key are for local development. Production startup rejects the sample JWT key; set a unique `Jwt:Key` of at least 32 characters through the deployment secret store.

## Hosted API and persistent database (test deployment)

The API can run in Docker with PostgreSQL and persistent Docker volumes for both the database and property media uploads:

1. Install Docker Compose on the host or hosting provider.
2. Copy `.env.example` to `.env` and set unique values for `POSTGRES_PASSWORD` and `JWT_KEY` (at least 32 random characters). Keep `.env` private; it is ignored by Git.
3. Set `WEB_ORIGIN` to the exact public frontend origin (for example, `https://your-site.vercel.app`) and set `FLUTTERWAVE_REDIRECT_URL` to that origin followed by `/payment/complete`.
4. For payment testing, enter Flutterwave **test** secret and webhook hash values in `.env`. Register `https://YOUR_API_HOST/api/payments/flutterwave/webhook` as the Flutterwave webhook URL. Without these credentials, checkout will report that payments need configuration. Never use live credentials for a test deployment.
5. Start the API and database with `docker compose up --build -d`. Check `http://YOUR_API_HOST:5188/health/ready`; it should return `{"status":"ready","database":"connected"}`.
6. Set the frontend build variable `NEXT_PUBLIC_API_URL` to the API origin without a trailing slash, rebuild/redeploy the frontend, and verify sign-in, listings, uploads and test checkout.

For a managed host, deploy `backend/NikSpeed.Api/Dockerfile`, provide the same environment variables through its secret/configuration panel, attach a managed PostgreSQL database, and mount durable storage at `/var/lib/nikspeed/uploads`. Configure its health check to `/health/ready`. The `Database__Provider` value must be `PostgreSQL`, and production refuses to start without PostgreSQL, a strong JWT key and a frontend origin. Database creation uses EF Core `EnsureCreated` for a fresh test database; use reviewed EF migrations before evolving an existing production schema.

The source repository does not provision an external hosting account or managed PostgreSQL service by itself. Creating those cloud resources requires access to the selected provider account; this Compose setup is ready to run on any Docker-capable host.
## Render test hosting

This repository includes a Render Blueprint in `render.yaml` for the Next.js site, the .NET API, and PostgreSQL in Singapore. To provision it, push the repository (including the Blueprint) to GitHub, create/sign in to a Render account, choose **New → Blueprint**, connect this repository and branch, review the three free services, and deploy. Render generates the JWT signing key and connects the PostgreSQL URL; no application password needs to be added to the YAML. The web/API domains are resolved from Render's generated external hostnames.

After deployment, configure `Flutterwave__SecretKey` and `Flutterwave__WebhookHash` on the API service using Flutterwave **test** credentials. Set its webhook to `https://YOUR_API_HOST/api/payments/flutterwave/webhook`. The API builds its payment return URL from the generated web origin. Checkout stays disabled until these provider values are set.

This is a short-lived test setup: Render's free PostgreSQL database is limited to 1 GB and expires 30 days after creation; free web services can sleep when idle and do not support persistent disks. Uploaded property media is therefore temporary and can disappear on a service restart or deploy. Do not use this free setup for real customer data or live payments. Persistent media storage and a longer-lived database require paid hosting or a separate object-storage service.

Render provisions the Blueprint only after the GitHub repository is pushed and connected to a Render account. No Render or GitHub credentials are available in this workspace, so the cloud resources still need to be created from the Render dashboard.
