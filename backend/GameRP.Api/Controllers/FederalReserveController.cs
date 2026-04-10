using Microsoft.AspNetCore.Mvc;
using GameRP.Api.DTOs;
using GameRP.Api.Services;

namespace GameRP.Api.Controllers;

/// <summary>
/// Federal Reserve endpoints — gold exchange and economic stats
/// </summary>
[ApiController]
[Route("api/federal-reserve")]
public class FederalReserveController : ControllerBase
{
    private readonly ILogger<FederalReserveController> _logger;
    private readonly FederalReserveService _federalReserveService;

    public FederalReserveController(ILogger<FederalReserveController> logger, FederalReserveService federalReserveService)
    {
        _logger = logger;
        _federalReserveService = federalReserveService;
    }

    /// <summary>
    /// Get current Federal Reserve economic stats
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(FederalReserveStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FederalReserveStatsDto>> GetStats()
    {
        var stats = await _federalReserveService.GetStatsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// Get historical Federal Reserve snapshots for graphing
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(FederalReserveHistoryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FederalReserveHistoryDto>> GetHistory([FromQuery] int periods = 10)
    {
        var history = await _federalReserveService.GetHistoryAsync(periods);
        return Ok(history);
    }

    /// <summary>
    /// Deposit gold bars at the Federal Reserve in exchange for currency
    /// </summary>
    [HttpPost("deposit-gold")]
    [ProducesResponseType(typeof(GoldOperationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GoldOperationResultDto>> DepositGold([FromBody] GoldDepositRequestDto request)
    {
        _logger.LogInformation("Gold deposit request: {GoldBars} bars from SteamID {SteamId}",
            request.GoldBars, request.SteamId);

        try
        {
            var result = await _federalReserveService.DepositGoldAsync(request.SteamId, request.GoldBars);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Gold deposit failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Withdraw gold bars from the Federal Reserve by paying currency
    /// </summary>
    [HttpPost("withdraw-gold")]
    [ProducesResponseType(typeof(GoldOperationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GoldOperationResultDto>> WithdrawGold([FromBody] GoldWithdrawRequestDto request)
    {
        _logger.LogInformation("Gold withdrawal request: {GoldBars} bars for SteamID {SteamId}",
            request.GoldBars, request.SteamId);

        try
        {
            var result = await _federalReserveService.WithdrawGoldAsync(request.SteamId, request.GoldBars);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Gold withdrawal failed: {Message}", ex.Message);
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
            message = "Federal Reserve API is running"
        });
    }
}
