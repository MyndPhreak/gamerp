# Quick Start Guide

Get the GameRP economy system running in 5 minutes!

## Step 1: Start the API Server

### On Windows:
```bash
cd backend
start-api.bat
```

### On Linux/Mac:
```bash
cd backend
chmod +x start-api.sh
./start-api.sh
```

### Or manually:
```bash
cd backend/GameRP.Api
dotnet run
```

**You should see:**
```
===========================================
GameRP Economy API Server
===========================================
Listening on: http://localhost:5000
Swagger UI: /swagger
===========================================
```

## Step 2: Test the API (Optional)

Open your browser to:
- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/api/wallet/health

You should see:
```json
{
  "status": "healthy",
  "timestamp": "2026-01-04T...",
  "totalWallets": 0,
  "message": "GameRP Economy API is running"
}
```

✅ **API is working!**

## Step 3: Copy Files to Your S&Box Project

Copy these files to your S&Box addon's `code/` directory:

```
gamemode/code/Economy/IEconomyApi.cs           → YourAddon/code/Economy/
gamemode/code/Economy/EconomyApiClient.cs      → YourAddon/code/Economy/
gamemode/code/Systems/EconomySystem.cs         → YourAddon/code/Systems/
gamemode/code/Commands/EconomyCommands.cs      → YourAddon/code/Commands/
```

**Example:**
```
MyGamemode/
└── code/
    ├── Economy/
    │   ├── IEconomyApi.cs
    │   └── EconomyApiClient.cs
    ├── Systems/
    │   └── EconomySystem.cs
    └── Commands/
        └── EconomyCommands.cs
```

## Step 4: Whitelist the API URL

In your S&Box project settings, add to the networking whitelist:
```
http://localhost:5000
```

**How to do this:**
1. Open your addon in the S&Box editor
2. Go to Project Settings
3. Find "Networking" or "HTTP Whitelist"
4. Add: `http://localhost:5000`

## Step 5: Launch S&Box and Test

1. **Start your gamemode** in S&Box
2. **Open the console** (` or F1)
3. **Run the test command:**

```
economy_test
```

**Expected output:**
```
========================================
Testing Economy API Connection...
========================================
[EconomyAPI] Health check: http://localhost:5000/api/wallet/health
[EconomyAPI] Health check passed: {"status":"healthy"...}
✓ API is online and responding!
========================================
```

✅ **Connection working!**

## Step 6: Check Your Balance

In the S&Box console, run:

```
economy_balance
```

**Expected output:**
```
========================================
Fetching wallet for SteamID: 76561198012345678
========================================
[EconomyAPI] Fetching wallet for SteamID: 76561198012345678
[EconomyAPI] Wallet retrieved successfully. Balance: $10000
Steam ID:      76561198012345678
Balance:       $10,000
Total Earned:  $10,000
Total Spent:   $0
Created:       1/4/2026 12:00:00 PM
Last Updated:  1/4/2026 12:00:00 PM
========================================
```

🎉 **You're all set!** The economy system is now connected and working!

## Available Commands

| Command | What it does |
|---------|--------------|
| `economy_test` | Test if API is responding |
| `economy_balance` | Get your wallet balance |
| `economy_getbalance <steamId>` | Get anyone's balance |

## Troubleshooting

### "API is not responding"

**Check:**
1. Is the API running? You should see it in the terminal
2. Is the URL correct? Should be `http://localhost:5000`
3. Is port 5000 blocked by firewall?

**Fix:**
```bash
cd backend/GameRP.Api
dotnet run
```

### "Failed to retrieve wallet data"

**Check:**
1. Did you whitelist `http://localhost:5000` in S&Box project settings?
2. Check S&Box console for error messages
3. Check API console for incoming requests

**Test directly:**
Open http://localhost:5000/api/wallet/76561198012345678 in your browser

### "Wallet data is null"

This means the HTTP request failed. Check:
1. CORS is enabled (it should be by default)
2. URL is whitelisted in S&Box
3. API is running

## Next Steps

Now that it's working, you can:

1. **View API documentation** - http://localhost:5000/swagger
2. **Read the full docs** - See [docs/](docs/) folder
3. **Add more features** - Follow [implementation guide](docs/implementation.md)
4. **Add database** - Replace in-memory storage with MSSQL

## Need Help?

- Check [README.md](README.md) for full documentation
- Check API logs in the terminal
- Check S&Box console for errors
- Read the troubleshooting section above

---

**Congratulations!** You now have a working economy API connected to S&Box! 🚀
