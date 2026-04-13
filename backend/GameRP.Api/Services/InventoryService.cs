using GameRP.Api.Data;
using GameRP.Api.DTOs;
using GameRP.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameRP.Api.Services;

public class InventoryService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService( ApplicationDbContext db, ILogger<InventoryService> logger )
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get a player's inventory. Creates one if it doesn't exist.
    /// </summary>
    public async Task<InventoryDto> GetOrCreateAsync( long steamId )
    {
        var player = await _db.Players.FirstOrDefaultAsync( p => p.SteamId == steamId );
        if ( player == null )
        {
            player = new Player { SteamId = steamId, DisplayName = steamId.ToString() };
            _db.Players.Add( player );
            await _db.SaveChangesAsync();
        }

        var inventory = await _db.Inventories
            .Include( i => i.Items )
            .FirstOrDefaultAsync( i => i.SteamId == steamId );

        if ( inventory == null )
        {
            inventory = new Inventory
            {
                PlayerId = player.Id,
                SteamId = steamId,
                MainSlots = 27,
                HotbarSlots = 9,
                SelectedHotbarSlot = 0
            };
            _db.Inventories.Add( inventory );
            await _db.SaveChangesAsync();
        }

        return ToDto( inventory );
    }

    /// <summary>
    /// Save (replace) a player's entire inventory state.
    /// Uses optimistic concurrency if RowVersion is provided.
    /// </summary>
    public async Task<InventoryDto> SaveAsync( long steamId, SaveInventoryRequest request )
    {
        var inventory = await _db.Inventories
            .Include( i => i.Items )
            .FirstOrDefaultAsync( i => i.SteamId == steamId );

        if ( inventory == null )
        {
            // Create the inventory if it doesn't exist
            var player = await _db.Players.FirstOrDefaultAsync( p => p.SteamId == steamId );
            if ( player == null )
            {
                player = new Player { SteamId = steamId, DisplayName = steamId.ToString() };
                _db.Players.Add( player );
                await _db.SaveChangesAsync();
            }

            inventory = new Inventory
            {
                PlayerId = player.Id,
                SteamId = steamId
            };
            _db.Inventories.Add( inventory );
        }

        // Optimistic concurrency check
        if ( !string.IsNullOrEmpty( request.RowVersion ) )
        {
            var clientVersion = Convert.FromBase64String( request.RowVersion );
            _db.Entry( inventory ).Property( i => i.RowVersion ).OriginalValue = clientVersion;
        }

        // Update meta
        inventory.MainSlots = request.MainSlots;
        inventory.HotbarSlots = request.HotbarSlots;
        inventory.SelectedHotbarSlot = request.SelectedHotbarSlot;

        // Replace items: delete existing, insert new
        // (Simpler than diff-tracking, since item lists tend to be small per player)
        _db.InventoryItems.RemoveRange( inventory.Items );
        inventory.Items.Clear();

        foreach ( var item in request.Items )
        {
            inventory.Items.Add( new InventoryItem
            {
                InventoryId = inventory.Id,
                Container = item.Container ?? "main",
                SlotIndex = item.SlotIndex,
                InstanceId = item.InstanceId ?? "",
                ItemId = item.ItemId,
                Quantity = item.Quantity,
                Durability = item.Durability,
                NbtJson = item.NbtJson ?? "{}"
            } );
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch ( DbUpdateConcurrencyException )
        {
            _logger.LogWarning( "Inventory concurrency conflict for SteamId {SteamId}", steamId );
            throw;
        }

        return ToDto( inventory );
    }

    /// <summary>
    /// Delete a player's inventory (soft delete via query filter)
    /// </summary>
    public async Task<bool> DeleteAsync( long steamId )
    {
        var inventory = await _db.Inventories
            .Include( i => i.Items )
            .FirstOrDefaultAsync( i => i.SteamId == steamId );

        if ( inventory == null ) return false;

        inventory.IsDeleted = true;
        inventory.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private static InventoryDto ToDto( Inventory inventory )
    {
        return new InventoryDto
        {
            SteamId = inventory.SteamId,
            MainSlots = inventory.MainSlots,
            HotbarSlots = inventory.HotbarSlots,
            SelectedHotbarSlot = inventory.SelectedHotbarSlot,
            RowVersion = inventory.RowVersion != null ? Convert.ToBase64String( inventory.RowVersion ) : "",
            Items = inventory.Items.Select( i => new InventoryItemDto
            {
                InstanceId = i.InstanceId,
                Container = i.Container,
                SlotIndex = i.SlotIndex,
                ItemId = i.ItemId,
                Quantity = i.Quantity,
                Durability = i.Durability,
                NbtJson = i.NbtJson
            } ).ToList()
        };
    }
}
