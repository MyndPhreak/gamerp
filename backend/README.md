# GameRP.Api - Economy Backend

ASP.NET Core Web API for the GameRP economy system.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or Rider (optional)

### Run the API

```bash
cd GameRP.Api
dotnet restore
dotnet run
```

The API will start on http://localhost:5000

### Swagger UI

Open http://localhost:5000/swagger to view and test the API endpoints.

## Available Endpoints

### GET /api/wallet/health
Health check endpoint

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2026-01-04T12:00:00Z",
  "totalWallets": 5,
  "message": "GameRP Economy API is running"
}
```

### GET /api/wallet/{steamId}
Get or create wallet for a player

**Parameters:**
- `steamId` (path) - Player's Steam ID (long)

**Response:**
```json
{
  "steamId": 76561198012345678,
  "balance": 10000,
  "totalEarned": 10000,
  "totalSpent": 0,
  "createdAt": "2026-01-04T12:00:00Z",
  "lastUpdated": "2026-01-04T12:00:00Z"
}
```

**Notes:**
- Auto-creates wallet with $10,000 starting balance if not found
- Currently uses in-memory storage (resets on restart)

### GET /api/wallet
Get all wallets (debug)

**Response:**
```json
[
  {
    "steamId": 76561198012345678,
    "balance": 10000,
    "totalEarned": 10000,
    "totalSpent": 0,
    "createdAt": "2026-01-04T12:00:00Z",
    "lastUpdated": "2026-01-04T12:00:00Z"
  }
]
```

## Project Structure

```
GameRP.Api/
├── Controllers/
│   └── WalletController.cs    # Wallet endpoints
├── Models/
│   └── WalletResponse.cs      # Response models
├── Program.cs                 # App configuration
├── appsettings.json          # Configuration
└── GameRP.Api.csproj         # Project file
```

## Development

### Watch Mode (Auto-reload)

```bash
dotnet watch run
```

### Build

```bash
dotnet build
```

### Publish

```bash
dotnet publish -c Release
```

## Configuration

Edit `appsettings.json`:

```json
{
  "urls": "http://localhost:5000",
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## CORS

CORS is configured to allow all origins for development. Update `Program.cs` for production.

## Next Steps

- [ ] Add Entity Framework Core
- [ ] Add MSSQL database
- [ ] Add authentication
- [ ] Add more endpoints (transfer, federal reserve, banks)
- [ ] Add validation
- [ ] Add rate limiting

## Testing

### Using curl

```bash
# Health check
curl http://localhost:5000/api/wallet/health

# Get wallet
curl http://localhost:5000/api/wallet/76561198012345678
```

### Using Swagger

Navigate to http://localhost:5000/swagger and use the interactive UI.

## Logs

Check console output for request logs:

```
info: GameRP.Api.Controllers.WalletController[0]
      GetWallet called for SteamID: 76561198012345678
info: GameRP.Api.Controllers.WalletController[0]
      Wallet found. Balance: $10000
```
