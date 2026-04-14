using System;

namespace GameRP.Inventory;

/// <summary>
/// A specific item instance — references an ItemDefinition by ID and holds
/// per-instance state (quantity, durability, NBT data, tick timer).
/// </summary>
public class ItemInstance
{
	/// <summary>
	/// Unique instance ID for tracking specific items
	/// </summary>
	public string InstanceId { get; set; } = Guid.NewGuid().ToString();

	/// <summary>
	/// The ItemDefinition this references
	/// </summary>
	public string ItemId { get; set; }

	/// <summary>
	/// How many in this stack
	/// </summary>
	public int Quantity { get; set; } = 1;

	/// <summary>
	/// Current durability. -1 = use definition default (= MaxDurability)
	/// </summary>
	public int Durability { get; set; } = -1;

	/// <summary>
	/// Per-instance NBT data
	/// </summary>
	public ItemNbt Nbt { get; set; } = new();

	/// <summary>
	/// Time accumulator for tick events. Reset by ItemTickSystem.
	/// </summary>
	/// <summary>
	/// Runtime only — not persisted
	/// </summary>
	public float TickAccumulator { get; set; } = 0f;

	public ItemInstance() { }

	public ItemInstance( string itemId, int quantity = 1 )
	{
		ItemId = itemId;
		Quantity = quantity;
	}

	/// <summary>
	/// Resolve the ItemDefinition from the registry
	/// </summary>
	public ItemDefinition GetDefinition()
	{
		return ItemRegistry.Get( ItemId );
	}

	/// <summary>
	/// Display name (uses NBT custom name if set, else definition name)
	/// </summary>
	public string GetDisplayName()
	{
		if ( !string.IsNullOrEmpty( Nbt?.CustomName ) )
			return Nbt.CustomName;
		return GetDefinition()?.Name ?? ItemId;
	}

	/// <summary>
	/// Get effective max durability
	/// </summary>
	public int GetMaxDurability()
	{
		return GetDefinition()?.MaxDurability ?? 0;
	}

	/// <summary>
	/// Get effective current durability (resolves -1 to max)
	/// </summary>
	public int GetCurrentDurability()
	{
		if ( Durability < 0 )
			return GetMaxDurability();
		return Durability;
	}

	/// <summary>
	/// Damage this item. Returns true if it broke.
	/// </summary>
	public bool DamageItem( int amount = 1 )
	{
		var max = GetMaxDurability();
		if ( max <= 0 ) return false;

		if ( Durability < 0 ) Durability = max;
		Durability -= amount;
		return Durability <= 0;
	}

	/// <summary>
	/// Repair this item by the given amount
	/// </summary>
	public void RepairItem( int amount = 1 )
	{
		var max = GetMaxDurability();
		if ( max <= 0 ) return;
		if ( Durability < 0 ) Durability = max;
		Durability = Math.Min( Durability + amount, max );
	}

	/// <summary>
	/// Whether this stack can merge with another (same item ID, both stackable, NBT compatible)
	/// </summary>
	public bool CanStackWith( ItemInstance other )
	{
		if ( other == null || other.ItemId != ItemId ) return false;

		var def = GetDefinition();
		if ( def == null || !def.IsStackable ) return false;

		// Don't stack damaged items
		if ( def.IsDamageable && (Durability != other.Durability) ) return false;

		// Don't stack items with different NBT custom names or modifiers
		if ( !string.IsNullOrEmpty( Nbt?.CustomName ) || !string.IsNullOrEmpty( other.Nbt?.CustomName ) ) return false;
		if ( Nbt?.Modifiers?.Count > 0 || other.Nbt?.Modifiers?.Count > 0 ) return false;

		return true;
	}

	/// <summary>
	/// Create a deep copy of this instance with a new instance ID
	/// </summary>
	public ItemInstance Clone()
	{
		return new ItemInstance
		{
			InstanceId = Guid.NewGuid().ToString(),
			ItemId = ItemId,
			Quantity = Quantity,
			Durability = Durability,
			Nbt = ItemNbt.Deserialize( Nbt?.Serialize() ?? "{}" )
		};
	}

	/// <summary>
	/// Split this stack — removes `amount` from this and returns a new stack
	/// </summary>
	public ItemInstance Split( int amount )
	{
		amount = Math.Clamp( amount, 1, Quantity );
		var split = Clone();
		split.Quantity = amount;
		Quantity -= amount;
		return split;
	}
}
