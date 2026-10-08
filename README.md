# Nik-Speed Properties LLC

A property marketplace for land, rentals, and homes for sale, operated by Nik-Speed Properties LLC. Advertisers buy listing credits before publishing properties.

## Projects

- `backend/NikSpeed.Api` — ASP.NET Core Web API with PostgreSQL/SQLite persistence, salted password hashes, private media storage, advertiser accounts, and Pesapal hosted checkout.
- `frontend` — Next.js 14 App Router frontend.

## Run locally

```powershell
cd backend/NikSpeed.Api
dotnet run
```

The API listens on `http://localhost:5188`. On its first run, it creates `nikspeed.db` and seeds example properties. Account emails and optional phone numbers, salted password hashes, properties, enquiries, reservations and payment references are stored in that database. Plain-text passwords are never stored. Phone accounts sign in with their account password; SMS verification requires a separate SMS provider.

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
dotnet user-secrets set "Pesapal:ConsumerKey" "your-sandbox-consumer-key"
dotnet user-secrets set "Pesapal:ConsumerSecret" "your-sandbox-consumer-secret"
dotnet user-secrets set "Pesapal:NotificationId" "registered-ipn-guid"
dotnet user-secrets set "Pesapal:CallbackUrl" "http://localhost:3000/payment/complete"
```

The payment API creates a Pesapal hosted checkout link when its consumer key, consumer secret, and registered notification ID are set. Register `GET https://YOUR_API_HOST/api/payments/pesapal/ipn` in Pesapal and use its returned IPN ID as `Pesapal:NotificationId`. The API checks each callback and IPN against Pesapal's transaction-status API, and requires a completed UGX payment matching the stored order and amount before issuing credits. Use the sandbox base URL for testing and the live base URL only after your Pesapal merchant account is approved. Deploy the API behind HTTPS with a managed database (PostgreSQL or SQL Server is recommended over SQLite).

The default SQLite database and JWT key are for local development. Production startup rejects the sample JWT key; set a unique `Jwt:Key` of at least 32 characters through the deployment secret store. Set `Database__Provider=PostgreSQL` in production; email addresses have a unique index, phone numbers are stored in international `+` format, and passwords use salted PBKDF2 hashes.

## Hosted API and persistent database (test deployment)

The API can run in Docker with PostgreSQL and persistent Docker volumes for both the database and property media uploads:

1. Install Docker Compose on the host or hosting provider.
2. Copy `.env.example` to `.env` and set unique values for `POSTGRES_PASSWORD` and `JWT_KEY` (at least 32 random characters). Keep `.env` private; it is ignored by Git.
3. Set `WEB_ORIGIN` to the exact public frontend origin (for example, `https://your-site.onrender.com`) and set `PESAPAL_CALLBACK_URL` to that origin followed by `/payment/complete`.
4. For payment testing, enter Pesapal sandbox credentials in `.env`. Register `https://YOUR_API_HOST/api/payments/pesapal/ipn` as a GET IPN URL in Pesapal, then set its returned ID as `PESAPAL_NOTIFICATION_ID`. Keep the sandbox base URL for tests; checkout stays disabled until the credentials and IPN ID are configured.
5. Start the API and database with `docker compose up --build -d`. Check `http://YOUR_API_HOST:5188/health/ready`; it should return `{"status":"ready","database":"connected"}`.
6. Set the frontend build variable `NEXT_PUBLIC_API_URL` to the API origin without a trailing slash, rebuild/redeploy the frontend, and verify sign-in, listings, uploads and test checkout.

For a managed host, deploy `backend/NikSpeed.Api/Dockerfile`, provide the same environment variables through its secret/configuration panel, attach a managed PostgreSQL database, and mount durable storage at `/var/lib/nikspeed/uploads`. Configure its health check to `/health/ready`. The `Database__Provider` value must be `PostgreSQL`, and production refuses to start without PostgreSQL, a strong JWT key and a frontend origin. Database creation uses EF Core `EnsureCreated` for a fresh test database; use reviewed EF migrations before evolving an existing production schema.

The source repository does not provision an external hosting account or managed PostgreSQL service by itself. Creating those cloud resources requires access to the selected provider account; this Compose setup is ready to run on any Docker-capable host.
## Render test hosting

This repository includes a Render Blueprint in `render.yaml` for the Next.js site, the .NET API, and PostgreSQL in Singapore. To provision it, push the repository (including the Blueprint) to GitHub, create/sign in to a Render account, choose **New → Blueprint**, connect this repository and branch, review the three free services, and deploy. Render generates the JWT signing key and connects the PostgreSQL URL; no application password needs to be added to the YAML. The web/API domains are resolved from Render's generated external hostnames.

After deployment, configure `Pesapal__ConsumerKey` and `Pesapal__ConsumerSecret` on the API service using Pesapal sandbox credentials. Register `https://YOUR_API_HOST/api/payments/pesapal/ipn` as a GET IPN URL with Pesapal and set its returned GUID as `Pesapal__NotificationId`. Keep `Pesapal__BaseUrl` set to `https://cybqa.pesapal.com/pesapalv3` for test checkout. Checkout stays disabled until these provider values are set. Switch to `https://pay.pesapal.com/v3` only when you are ready to use approved live merchant credentials.

This is a short-lived test setup: Render's free PostgreSQL database is limited to 1 GB and expires 30 days after creation; free web services can sleep when idle and do not support persistent disks. The Blueprint uses local media storage for the preview, so uploaded files can disappear when the service restarts or redeploys. Do not use this free preview for customer records, permanent property uploads, or live payments. Configure durable object storage and a longer-lived database before production use.

## Database and uploads

The API stores structured data in the configured relational database: account emails and phone numbers, salted password hashes, property metadata and media keys, enquiries, reservations and payment records. Card numbers, CVV and payment passwords must never be collected or written to this database; payment details stay on Pesapal's hosted checkout.

Local development stores uploaded media under `backend/NikSpeed.Api/App_Data/uploads`. Docker Compose uses PostgreSQL plus a durable `uploads-data` volume. Production media storage can use a private S3-compatible bucket such as AWS S3, Cloudflare R2 or Backblaze B2 with `Storage__Provider=S3` and the `Storage__S3__*` secrets. Uploads are checked for supported file types, size and media signatures before storage, and are served through the API. The free Render preview uses local storage, so uploaded files may disappear when its web service restarts or redeploys.

Render provisions the Blueprint only after the GitHub repository is pushed and connected to a Render account. No Render or GitHub credentials are available in this workspace, so the cloud resources still need to be created from the Render dashboard.
