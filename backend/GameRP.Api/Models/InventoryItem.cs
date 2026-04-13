using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameRP.Api.Models;

/// <summary>
/// A single item stack in an inventory slot. NBT data is stored as JSON.
/// </summary>
public class InventoryItem : BaseEntity
{
    [Required]
    public Guid InventoryId { get; set; }

    /// <summary>
    /// Which container this slot belongs to: "main", "hotbar", or any other named container
    /// </summary>
    [Required]
    [MaxLength( 32 )]
    public string Container { get; set; } = "main";

    /// <summary>
    /// Slot index within the container
    /// </summary>
    [Required]
    public int SlotIndex { get; set; }

    /// <summary>
    /// Client-side instance ID (for stable identity across save/load)
    /// </summary>
    [MaxLength( 64 )]
    public string InstanceId { get; set; } = "";

    /// <summary>
    /// Item definition ID from ItemRegistry
    /// </summary>
    [Required]
    [MaxLength( 64 )]
    public string ItemId { get; set; } = "";

    /// <summary>
    /// Stack quantity
    /// </summary>
    [Required]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Current durability (-1 = max/default)
    /// </summary>
    public int Durability { get; set; } = -1;

    /// <summary>
    /// Serialized NBT JSON blob
    /// </summary>
    [Column( TypeName = "nvarchar(max)" )]
    public string NbtJson { get; set; } = "{}";

    [ForeignKey( nameof( InventoryId ) )]
    public Inventory? Inventory { get; set; }

    public InventoryItem()
    {
        Id = NewGuidV7();
    }
}
