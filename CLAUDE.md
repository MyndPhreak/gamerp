# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GameRP is an S&Box (Source 2 engine) roleplay gamemode with a gold-backed economy system. It has two main parts:
- **S&Box gamemode** (`code/`) - C# game code running in S&Box's component-based architecture
- **ASP.NET Core 8.0 Web API** (`backend/GameRP.Api/`) - Economy backend with EF Core + MSSQL

The gamemode communicates with the backend over HTTP (localhost:8080) to manage player wallets, transactions, deposits, withdrawals, and transfers.

## Build & Run Commands

### Backend API
```bash
cd backend/GameRP.Api
dotnet restore          # restore NuGet packages
dotnet build            # build
dotnet run              # run (listens on http://localhost:8080)
dotnet watch run        # run with auto-reload
```

### Database Migrations (EF Core)
```bash
cd backend/GameRP.Api
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

Database: MSSQL LocalDB (`(localdb)\mssqllocaldb`, database `GameRP`).

### S&Box Gamemode
The gamemode compiles within the S&Box editor. There is no standalone dotnet build for `code/`. The project file is `gamerp.sbproj`.

### Testing
No formal test framework is set up. Manual testing via:
- Swagger UI at http://localhost:8080/swagger (when API is running)
- S&Box console commands: `economy_test`, `economy_balance`, `economy_getbalance <steamId>`

## Architecture

### Backend (Controllers -> Services -> EF Core)

```
WalletController  ->  WalletService  ->  ApplicationDbContext  ->  MSSQL
     (API)           (business logic)      (EF Core)
```

- **Models**: `Player`, `Wallet`, `Transaction` with `BaseEntity` (soft delete, timestamps, optimistic concurrency via `RowVersion`, GUIDs v7)
- **DTOs**: Separate request/response DTOs (`WalletDto`, `DepositRequestDto`, etc.)
- **DI**: `WalletService` registered as scoped; `ApplicationDbContext` via EF Core

### Gamemode (S&Box Component System)

- **`RPManager`** - Main game manager (payday system, RP mechanics)
- **`RPPlayer`** - Player component (movement, camera, animation, database persistence)
- **`EconomySystem`** - Static facade over `IEconomyApi`/`EconomyApiClient` for HTTP calls to the backend
- **`DatabaseService`** - Pluggable player persistence (Local JSON, Appwrite, or External) via `IPlayerDatabase` interface, configured as a Component with S&Box editor properties

### Key Patterns
- S&Box code uses `Sandbox` namespace and component lifecycle (`OnAwake`, `OnUpdate`, etc.)
- S&Box HTTP calls use `Http.RequestAsync()` (not `HttpClient`)
- Singleton access via static `Instance` property on components (e.g., `DatabaseService.Instance`)
- Lazy initialization for `EconomySystem` API client
- Backend uses query filters for soft delete, automatic `CreatedAt`/`UpdatedAt` in `SaveChangesAsync`

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/wallet/health` | Health check |
| GET | `/api/wallet/{steamId}` | Get or create wallet |
| POST | `/api/wallet/{steamId}/deposit` | Deposit money |
| POST | `/api/wallet/{steamId}/withdraw` | Withdraw money |
| POST | `/api/wallet/{steamId}/transfer` | Transfer to another player |

## Important Notes

- The API port is **8080** (configured in `appsettings.json`), not 5000 as some README files state
- CORS is configured to allow all origins (development only)
- S&Box project must whitelist `http://localhost:8080` in project settings for HTTP calls to work
- The `code/` directory is S&Box gamemode code - it references `Sandbox` APIs, not standard .NET
