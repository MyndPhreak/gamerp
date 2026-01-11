using Microsoft.AspNetCore.Mvc;
using GameRP.Api.DTOs;
using GameRP.Api.Services;

namespace GameRP.Api.Controllers;

/// <summary>
/// Wallet management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly ILogger<WalletController> _logger;
    private readonly WalletService _walletService;

    public WalletController(ILogger<WalletController> logger, WalletService walletService)
    {
        _logger = logger;
        _walletService = walletService;
    }

    /// <summary>
    /// Get or create wallet for a player
    /// </summary>
    /// <param name="steamId">Player's Steam ID</param>
    /// <param name="displayName">Optional display name for new players</param>
    /// <returns>Wallet data</returns>
    [HttpGet("{steamId}")]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WalletDto>> GetWallet(long steamId, [FromQuery] string? displayName = null)
    {
        _logger.LogInformation("GetWallet called for SteamID: {SteamId}", steamId);

        var wallet = await _walletService.GetOrCreateWalletAsync(steamId, displayName);

        if (wallet == null)
        {
            return NotFound(new { message = "Failed to get or create wallet" });
        }

        _logger.LogInformation("Wallet found/created. Balance: ${Balance}", wallet.Balance);
        return Ok(wallet);
    }

    /// <summary>
    /// Deposit money into a wallet
    /// </summary>
    /// <param name="steamId">Player's Steam ID</param>
    /// <param name="request">Deposit details</param>
    /// <returns>Updated wallet data</returns>
    [HttpPost("{steamId}/deposit")]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletDto>> Deposit(long steamId, [FromBody] DepositRequestDto request)
    {
        _logger.LogInformation("Deposit {Amount} for SteamID: {SteamId}", request.Amount, steamId);

        var wallet = await _walletService.DepositAsync(steamId, request);

        if (wallet == null)
        {
            return NotFound(new { message = "Wallet not found" });
        }

        return Ok(wallet);
    }

    /// <summary>
    /// Withdraw money from a wallet
    /// </summary>
    /// <param name="steamId">Player's Steam ID</param>
    /// <param name="request">Withdrawal details</param>
    /// <returns>Updated wallet data</returns>
    [HttpPost("{steamId}/withdraw")]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WalletDto>> Withdraw(long steamId, [FromBody] WithdrawRequestDto request)
    {
        _logger.LogInformation("Withdraw {Amount} for SteamID: {SteamId}", request.Amount, steamId);

        try
        {
            var wallet = await _walletService.WithdrawAsync(steamId, request);

            if (wallet == null)
            {
                return NotFound(new { message = "Wallet not found" });
            }

            return Ok(wallet);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Withdrawal failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Transfer money to another player
    /// </summary>
    /// <param name="steamId">Sender's Steam ID</param>
    /// <param name="request">Transfer details</param>
    /// <returns>Updated sender wallet data</returns>
    [HttpPost("{steamId}/transfer")]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WalletDto>> Transfer(long steamId, [FromBody] TransferRequestDto request)
    {
        _logger.LogInformation("Transfer {Amount} from {FromSteamId} to {ToSteamId}",
            request.Amount, steamId, request.ToSteamId);

        try 
        {
            var wallet = await _walletService.TransferAsync(steamId, request);

            if (wallet == null)
            {
                return NotFound(new { message = "Sender wallet not found" });
            }

            return Ok(wallet);
        } 
        catch (InvalidOperationException ex) 
        {
            _logger.LogWarning("Transfer failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
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
            message = "GameRP Economy API is running with database"
        });
    }
}
