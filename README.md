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

### 1. Backend Web API (`WebApplication1`)

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

### 2. Cloudflare Worker (`worker`)

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