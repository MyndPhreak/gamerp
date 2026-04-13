using GameRP.Api.DTOs;
using GameRP.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameRP.Api.Controllers;

[ApiController]
[Route( "api/[controller]" )]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _service;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController( InventoryService service, ILogger<InventoryController> logger )
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get a player's inventory (creates one if missing)
    /// </summary>
    [HttpGet( "{steamId}" )]
    public async Task<ActionResult<InventoryDto>> Get( long steamId )
    {
        try
        {
            var inv = await _service.GetOrCreateAsync( steamId );
            return Ok( inv );
        }
        catch ( Exception ex )
        {
            _logger.LogError( ex, "Failed to get inventory for {SteamId}", steamId );
            return StatusCode( 500, new { error = ex.Message } );
        }
    }

    /// <summary>
    /// Save a player's full inventory state.
    /// Optionally pass a RowVersion to enable optimistic concurrency.
    /// </summary>
    [HttpPost( "{steamId}/save" )]
    public async Task<ActionResult<InventoryDto>> Save( long steamId, [FromBody] SaveInventoryRequest request )
    {
        try
        {
            var inv = await _service.SaveAsync( steamId, request );
            return Ok( inv );
        }
        catch ( Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException )
        {
            return Conflict( new { error = "Inventory was modified by another process. Reload and try again." } );
        }
        catch ( Exception ex )
        {
            _logger.LogError( ex, "Failed to save inventory for {SteamId}", steamId );
            return StatusCode( 500, new { error = ex.Message } );
        }
    }

    /// <summary>
    /// Soft-delete a player's inventory
    /// </summary>
    [HttpDelete( "{steamId}" )]
    public async Task<IActionResult> Delete( long steamId )
    {
        var deleted = await _service.DeleteAsync( steamId );
        if ( !deleted ) return NotFound();
        return NoContent();
    }
}
