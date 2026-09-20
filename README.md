# CheaterWatch (Steam Account Tracker)

CheaterWatch is a scalable, full stack application that allows users to track Steam accounts and receive email notifications when those accounts receive a VAC, Game, or Community ban.

## Architecture & Tech Stack

This project is built for high performance and scalability using a decoupled architecture:

*   **Frontend**: React 19 Single Page Application built with Vite and TailwindCSS.
*   **Backend API**: ASP.NET Core Minimal API (.NET 8/10) providing highly secure, stateful session authentication and strict rate-limiting.
*   **Database**: Microsoft SQL Server.
*   **Background Worker**: A serverless Cloudflare Worker (TypeScript) triggered via Cron. 
*   **Emails**: Integrated with the Resend API for reliable ban notifications.
*   **Observability**: Integrated with Sentry for full-stack error tracking.

## Features

*   **Smart Tracking**: Input any Steam ID format (Vanity URLs, Profile URLs, Legacy STEAM_0:1:X, or standard SteamId64), and the backend automatically resolves and tracks the correct account.
*   **Dashboard**: View all your tracked accounts and see exactly how many other CheaterWatch users are tracking the same suspect.
*   **Worker Queue**: Cloudflare Workers have strict execution time limits on the free tier. To bypass this limitation the worker utilizes a "Sliding Window Queue". It explicitly monitors its own execution metrics. If it reaches its execution limits, it gracefully halts, marks the processed accounts as scanned (pushing them to the back of the line), and safely exits. The next Cron trigger picks up the remaining accounts exactly where it left off.
*   **Enterprise Security**: Employs strictly typed HttpOnly SameSite=Strict session cookies (native CSRF protection), Bcrypt password hashing, internal Worker API keys, and endpoint rate limiting.

---

## Local Development Setup

### Prerequisites
*   .NET 10 SDK
*   Node.js & npm
*   Docker (Optional, for running SQL Server locally)

### 1. Database Configuration
Run a local SQL Server instance (e.g., via Docker):
```bash
docker run -e 'ACCEPT_EULA=Y' -e 'MSSQL_SA_PASSWORD=YourStrongPassword123!' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```
Apply the database migrations in order:
1. Run `WebApplication1/Migrations/000_InitialCreate.sql`
2. Run `WebApplication1/Migrations/001_AddBanTrackingColumns.sql`

### 2. Backend API
Navigate to the backend directory and configure your secrets (or update appsettings.Development.json):
```bash
cd WebApplication1
dotnet user-secrets set "ConnectionStrings:CONNECTION_STRING" "Server=localhost,1433;Database=master;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;"
dotnet user-secrets set "STEAM_API_KEY" "your_steam_api_key"
dotnet user-secrets set "WORKER_KEY" "super_secret_internal_key"
dotnet run
```

### 3. React Frontend
In a new terminal, navigate to the frontend directory:
```bash
cd frontend
npm install
npm run dev
```

### 4. Cloudflare Worker (Scanner)_
In a third terminal, navigate to the worker directory. Copy .dev.vars.example (or configure Wrangler directly) to set your STEAM_API_KEY, RESEND_API_KEY, BASE_API and WORKER_KEY:
```bash
cd worker
npm install
npx wrangler dev --test-scheduled
```

---

## Production Deployment

This repository includes a `docker-compose.prod.yml` configured for production deployment on a VPS (e.g., DigitalOcean). It automatically provisions:
1.  **SQL Server**: Persistent database container.
2.  **API**: The ASP.NET Core backend.
3.  **Caddy**: An edge proxy that automatically provisions and renews free SSL certificates via Let's Encrypt.

To deploy:
```bash
docker compose -f docker-compose.prod.yml up -d
```

## Testing
The backend has a robust xUnit test suite covering core business logic, session validation, authentication flows, and SteamID conversions.

```bash
dotnet test WebApplication1.sln
```
*Note: GitHub Actions is configured to automatically run these tests on every push to the master branch.*
