using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameRP.Api.Models;

/// <summary>
/// A persistent player inventory. One per player. Has multiple containers
/// (Main, Hotbar, etc.) each represented by InventoryItem rows.
/// </summary>
public class Inventory : BaseEntity
{
    [Required]
    public Guid PlayerId { get; set; }

    [Required]
    public long SteamId { get; set; }

    /// <summary>
    /// Number of slots in the main container
    /// </summary>
    public int MainSlots { get; set; } = 27;

    /// <summary>
    /// Number of slots in the hotbar
    /// </summary>
    public int HotbarSlots { get; set; } = 9;

    /// <summary>
    /// Currently selected hotbar slot
    /// </summary>
    public int SelectedHotbarSlot { get; set; } = 0;

    [ForeignKey( nameof( PlayerId ) )]
    public Player? Player { get; set; }

    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();

    public Inventory()
    {
        Id = NewGuidV7();
    }
}
