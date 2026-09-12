# SteamAccountTracker

SteamAccountTracker is a system for tracking Steam accounts and monitoring them for ban status changes.

## Architecture

The project consists of two main components:
1.  **WebApplication1 (Backend API)**: A .NET 8 ASP.NET Core Web API that handles user authentication, session management, and Steam ID conversion/resolution.
2.  **worker (Background Scanner)**: A Cloudflare Worker written in TypeScript that periodically scans tracked Steam accounts for ban status changes and sends email notifications.

## Prerequisites

To run this project locally, you will need:
-   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or higher, e.g., .NET 10 is supported via RollForward).
-   [Node.js](https://nodejs.org/) (for the Cloudflare Worker).
-   A [Steam Web API Key](https://steamcommunity.com/dev/apikey) for fetching data from Steam.

## Local Setup

### 1. Database Setup (SQL Server)

This project requires a SQL Server database.

1. Create a new SQL Server database (e.g., using SQL Server Management Studio, Azure Data Studio, or Docker).
2. Apply the database migrations found in `WebApplication1/Migrations/` in order:
   - Run `000_InitialCreate.sql` to create the base tables (`Users`, `SteamAccounts`, `UserSteamAccounts`).
   - Run `001_AddBanTrackingColumns.sql` to add ban tracking metadata.
3. Configure your connection string. You can add it to `WebApplication1/appsettings.Development.json` or use .NET User Secrets:
   ```json
   {
     "ConnectionStrings": {
       "CONNECTION_STRING": "Server=localhost;Database=SteamTracker;User Id=sa;Password=YourPassword123;TrustServerCertificate=True;"
     }
   }
   ```

### 2. Backend Web API (`WebApplication1`)

1.  Navigate to the `WebApplication1` directory:
    ```bash
    cd WebApplication1
    ```
2.  Configure your Steam API Key. Edit `appsettings.Development.json` (or set a user secret) and ensure your `Steam:ApiKey` is set:
    ```json
    {
      "Steam": {
        "ApiKey": "YOUR_STEAM_API_KEY_HERE"
      }
    }
    ```
3.  Run the API:
    ```bash
    dotnet run
    ```

### 3. Cloudflare Worker (`worker`)

1.  Navigate to the `worker` directory:
    ```bash
    cd worker
    ```
2.  Install dependencies:
    ```bash
    npm install
    ```
3.  Run the worker locally using Wrangler:
    ```bash
    npm run dev
    ```

## Testing

The project uses xUnit for testing the .NET backend. The tests cover core logic such as user authentication and Steam ID conversions (including legacy ID math and vanity URL resolution) without relying on external network requests.

To run the test suite from the root directory:
```bash
dotnet test WebApplication1.sln
```

*(Note: The test project is configured with `<RollForward>Major</RollForward>`, allowing it to run even if you have a newer SDK like .NET 10 installed.)*

## CI/CD

This repository is configured with GitHub Actions.
-   **Unit Tests**: Automatically restore, build, and test the .NET solution on every `push` and `pull_request` to the `master` branch. Test results (`.trx` files) are uploaded as pipeline artifacts.