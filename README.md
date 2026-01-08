# GameRP - Economy System

A comprehensive gold-backed economy system for S&Box with an external ASP.NET Core Web API backend.

## Project Structure

```
gamerp/
├── backend/                    # ASP.NET Core Web API
│   └── GameRP.Api/
│       ├── Controllers/        # API endpoints
│       ├── Models/             # Data models
│       ├── Program.cs          # API entry point
│       └── GameRP.Api.csproj
│
├── gamemode/                   # S&Box gamemode
│   └── code/
│       ├── Economy/            # Economy API client
│       │   ├── IEconomyApi.cs
│       │   └── EconomyApiClient.cs
│       ├── Systems/            # Game systems
│       │   └── EconomySystem.cs
│       └── Commands/           # Console commands
│           └── EconomyCommands.cs
│
└── docs/                       # Documentation
    ├── overview.md
    ├── implementation.md
    ├── event-sourcing-architecture.md
    └── backend-api-architecture.md
```

## Quick Start

### 1. Start the API Server

```bash
cd backend/GameRP.Api
dotnet run
```

The API will start on `http://localhost:5000`

- Swagger UI: http://localhost:5000/swagger
- Health check: http://localhost:5000/api/wallet/health

### 2. Add Gamemode Code to Your S&Box Project

Copy the files from `gamemode/code/` into your S&Box addon's code directory.

### 3. Whitelist the API URL in S&Box

In your S&Box project settings (`.sbproj` or project config), whitelist:
```
http://localhost:5000
```

### 4. Test the Connection

In S&Box console, run:
```
economy_test
```

This will test the API connection and show if it's working.

### 5. Check Your Balance

In S&Box console, run:
```
economy_balance
```

This will fetch your wallet from the API and display:
- Steam ID
- Current balance
- Total earned
- Total spent
- Account creation date

## Available Commands

### Server Commands (S&Box)

| Command | Description |
|---------|-------------|
| `economy_test` | Test API health check |
| `economy_balance` | Get your current wallet balance |
| `economy_getbalance <steamId>` | Get balance for any Steam ID |

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/wallet/health` | Health check |
| GET | `/api/wallet/{steamId}` | Get wallet for player |
| GET | `/api/wallet` | Get all wallets (debug) |

## How It Works

### Architecture

```
┌─────────────┐         HTTP          ┌─────────────┐
│   S&Box     │ ──────────────────→   │  ASP.NET    │
│  Gamemode   │                       │  Web API    │
│             │ ←──────────────────   │             │
└─────────────┘      JSON Data        └─────────────┘
      │                                      │
      │                                      │
   Players                            In-Memory Storage
                                    (will become database)
```

### Flow

1. **S&Box Gamemode** calls `EconomySystem.Api.GetWalletAsync(steamId)`
2. **EconomyApiClient** makes HTTP request to `http://localhost:5000/api/wallet/{steamId}`
3. **WalletController** receives request, looks up or creates wallet
4. **API** returns JSON with wallet data
5. **S&Box** deserializes JSON into `WalletData` struct
6. **Console** displays the balance

### Example Code Usage

```csharp
// In your S&Box gamemode
using GameRP.Systems;

public class MyGamemode : GameManager
{
    public override void ClientJoined( IClient client )
    {
        base.ClientJoined( client );

        // Get player's balance when they join
        _ = CheckPlayerBalance( client );
    }

    private async Task CheckPlayerBalance( IClient client )
    {
        var balance = await EconomySystem.GetBalance( client.SteamId );
        Log.Info( $"{client.Name} has ${balance}" );
    }
}
```

## Current Features

✅ ASP.NET Core Web API backend
✅ Simple wallet endpoint
✅ Health check endpoint
✅ S&Box API client interface
✅ HTTP communication via `Http.RequestAsync`
✅ Console commands for testing
✅ Auto-create wallets with starting balance
✅ In-memory storage (temporary)

## Next Steps

### Phase 1: Database Integration
- [ ] Add EF Core and MSSQL
- [ ] Create database migrations
- [ ] Replace in-memory storage

### Phase 2: More Endpoints
- [ ] POST `/api/wallet/transfer` - Transfer money
- [ ] POST `/api/federalreserve/deposit-gold` - Sell gold
- [ ] GET `/api/bank/{id}` - Get bank info

### Phase 3: S&Box Integration
- [ ] Player component with networked balance
- [ ] HUD showing player balance
- [ ] Mining system integration
- [ ] NPC shops that call API

### Phase 4: Security
- [ ] JWT authentication
- [ ] Rate limiting
- [ ] Input validation
- [ ] API key for S&Box server

## Troubleshooting

### API Not Responding

**Problem:** `economy_test` shows API is not responding

**Solution:**
1. Make sure API is running: `cd backend/GameRP.Api && dotnet run`
2. Check URL is correct: `http://localhost:5000`
3. Check firewall isn't blocking port 5000
4. Verify CORS is enabled in API

### Wallet Data is Null

**Problem:** `economy_balance` shows "Failed to retrieve wallet data"

**Solutions:**
1. Check API logs for errors
2. Verify Steam ID is valid (should be a long number)
3. Test endpoint directly: http://localhost:5000/api/wallet/76561198012345678
4. Check S&Box console for error messages

### URL Not Whitelisted

**Problem:** S&Box blocks HTTP request

**Solution:**
Add `http://localhost:5000` to your project's whitelisted URLs in project settings.

## Development

### Run API in Development Mode

```bash
cd backend/GameRP.Api
dotnet watch run
```

This will auto-reload when you change code.

### View API Documentation

Open http://localhost:5000/swagger while API is running.

### Test API with curl

```bash
# Health check
curl http://localhost:5000/api/wallet/health

# Get wallet
curl http://localhost:5000/api/wallet/76561198012345678

# Get all wallets
curl http://localhost:5000/api/wallet
```

## Documentation

Full documentation is available in the [docs](docs/) folder:

- [System Overview](docs/overview.md)
- [Implementation Guide](docs/implementation.md)
- [Event Sourcing Architecture](docs/event-sourcing-architecture.md)
- [Backend API Architecture](docs/backend-api-architecture.md)
- [Federal Reserve System](docs/federal-reserve.md)
- [Banking System](docs/banking-system.md)
- [Tax System](docs/tax-system.md)
- And more...

## Technology Stack

**Backend:**
- ASP.NET Core 8.0
- Entity Framework Core (coming soon)
- MSSQL Server (coming soon)
- Swagger/OpenAPI

**Gamemode:**
- S&Box / Source 2
- C# (.NET)
- HTTP client

## Contributing

This is the beginning of a comprehensive economy system. See the docs folder for the full design.

## License

[Your License]

---

**Status:** 🚧 Early Development - API + S&Box integration working!
