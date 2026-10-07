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
