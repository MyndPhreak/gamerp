using Microsoft.AspNetCore.Mvc;
using GameRP.Api.Models;

namespace GameRP.Api.Controllers;

/// <summary>
/// Wallet management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly ILogger<WalletController> _logger;

    // In-memory storage for testing (will be replaced with database)
    private static readonly Dictionary<long, WalletResponse> _wallets = new();

    public WalletController(ILogger<WalletController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get wallet information for a player
    /// </summary>
    /// <param name="steamId">Player's Steam ID</param>
    /// <returns>Wallet data</returns>
    [HttpGet("{steamId}")]
    [ProducesResponseType(typeof(WalletResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<WalletResponse> GetWallet(long steamId)
    {
        _logger.LogInformation("GetWallet called for SteamID: {SteamId}", steamId);

        // Check if wallet exists
        if (_wallets.TryGetValue(steamId, out var wallet))
        {
            _logger.LogInformation("Wallet found. Balance: ${Balance}", wallet.Balance);
            return Ok(wallet);
        }

        // Auto-create wallet with starting balance for testing
        var newWallet = new WalletResponse
        {
            SteamId = steamId,
            Balance = 10000, // Starting balance for testing
            TotalEarned = 10000,
            TotalSpent = 100,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        _wallets[steamId] = newWallet;

        _logger.LogInformation("New wallet created for SteamID: {SteamId} with balance ${Balance}",
            steamId, newWallet.Balance);

        return Ok(newWallet);
    }

    /// <summary>
    /// Get all wallets (for testing/debugging)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WalletResponse>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<WalletResponse>> GetAllWallets()
    {
        _logger.LogInformation("GetAllWallets called. Total wallets: {Count}", _wallets.Count);
        return Ok(_wallets.Values);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            totalWallets = _wallets.Count,
            message = "GameRP Economy API is running"
        });
    }
}
