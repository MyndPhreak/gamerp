using System;

namespace GameRP.Inventory;

/// <summary>
/// Template defining a type of item. ItemInstances reference this by ID.
/// </summary>
public class ItemDefinition
{
	/// <summary>
	/// Unique ID (e.g. "food.burger", "weapon.pistol")
	/// </summary>
	public string Id { get; set; }

	/// <summary>
	/// Display name
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Description for tooltip
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// Category for sorting/filtering
	/// </summary>
	public string Category { get; set; }

	/// <summary>
	/// Material Icon name for UI
	/// </summary>
	public string Icon { get; set; } = "inventory_2";

	/// <summary>
	/// Rarity tier
	/// </summary>
	public ItemRarity Rarity { get; set; } = ItemRarity.Common;

	/// <summary>
	/// Maximum stack size. 1 = non-stackable.
	/// </summary>
	public int MaxStack { get; set; } = 1;

	/// <summary>
	/// Weight per unit
	/// </summary>
	public float Weight { get; set; } = 0f;

	/// <summary>
	/// Default value (what NPCs pay for it)
	/// </summary>
	public int BaseValue { get; set; } = 0;

	/// <summary>
	/// Maximum durability. 0 = indestructible/non-damageable.
	/// </summary>
	public int MaxDurability { get; set; } = 0;

	/// <summary>
	/// World model when dropped
	/// </summary>
	public string WorldModel { get; set; }

	/// <summary>
	/// Prefab to spawn when used
	/// </summary>
	public string PrefabPath { get; set; }

	/// <summary>
	/// Clothing path for wearables
	/// </summary>
	public string ClothingPath { get; set; }

	/// <summary>
	/// Whether the item is consumed on use
	/// </summary>
	public bool Consumable { get; set; } = false;

	/// <summary>
	/// Comma-separated tags ("food,illegal,quest")
	/// </summary>
	public string Tags { get; set; } = "";

	/// <summary>
	/// How often this item ticks while in an inventory (seconds).
	/// 0 = no ticking. Use for spoiling food, draining batteries, etc.
	/// </summary>
	public float TickInterval { get; set; } = 0f;

	/// <summary>
	/// Optional handler ID for use logic. Looked up in ItemUseRegistry.
	/// </summary>
	public string UseHandlerId { get; set; }

	/// <summary>
	/// Optional handler ID for tick logic. Looked up in ItemTickRegistry.
	/// </summary>
	public string TickHandlerId { get; set; }

	public bool HasTag( string tag )
	{
		if ( string.IsNullOrEmpty( Tags ) || string.IsNullOrEmpty( tag ) )
			return false;
		return Tags.Contains( tag, StringComparison.OrdinalIgnoreCase );
	}

	public bool IsStackable => MaxStack > 1;
	public bool IsDamageable => MaxDurability > 0;
	public bool IsTickable => TickInterval > 0f;
}
