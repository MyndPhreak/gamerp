using System;

namespace GameRP.Inventory;

/// <summary>
/// A single inventory slot. Holds an ItemInstance or null.
/// Can have filters that restrict what can be placed in it (for hotbars,
/// special slots, etc.) and a locked state.
/// </summary>
public class ItemSlot
{
	/// <summary>
	/// The item in this slot, or null if empty
	/// </summary>
	public ItemInstance Item { get; set; }

	/// <summary>
	/// Index in the parent container
	/// </summary>
	public int Index { get; set; }

	/// <summary>
	/// If locked, items cannot be moved in or out via UI (but can be modified by code)
	/// </summary>
	public bool Locked { get; set; } = false;

	/// <summary>
	/// Optional filter — if set, only items matching this predicate can be placed.
	/// Used for slots that only accept certain item types (e.g. ammo-only quickslot).
	/// </summary>
	public Func<ItemInstance, bool> Filter { get; set; }

	/// <summary>
	/// Whether this slot is empty
	/// </summary>
	public bool IsEmpty => Item == null || Item.Quantity <= 0;

	/// <summary>
	/// Check if an item can be placed in this slot
	/// </summary>
	public bool CanAccept( ItemInstance item )
	{
		if ( Locked ) return false;
		if ( item == null ) return true;
		if ( Filter != null && !Filter( item ) ) return false;
		return true;
	}

	/// <summary>
	/// Try to merge an incoming item into this slot. Returns the leftover (or null if fully merged).
	/// </summary>
	public ItemInstance MergeOrPlace( ItemInstance incoming )
	{
		if ( !CanAccept( incoming ) ) return incoming;

		// Empty slot — place the whole stack
		if ( IsEmpty )
		{
			Item = incoming;
			return null;
		}

		// Same item type — merge stacks
		if ( Item.CanStackWith( incoming ) )
		{
			var def = Item.GetDefinition();
			int max = def?.MaxStack ?? 1;
			int space = max - Item.Quantity;

			if ( space <= 0 ) return incoming;

			int toMove = Math.Min( space, incoming.Quantity );
			Item.Quantity += toMove;
			incoming.Quantity -= toMove;

			if ( incoming.Quantity <= 0 ) return null;
			return incoming;
		}

		return incoming;
	}

	/// <summary>
	/// Take all items from this slot
	/// </summary>
	public ItemInstance TakeAll()
	{
		if ( IsEmpty ) return null;
		var item = Item;
		Item = null;
		return item;
	}

	/// <summary>
	/// Take a specific quantity from this slot
	/// </summary>
	public ItemInstance Take( int quantity )
	{
		if ( IsEmpty ) return null;
		if ( quantity >= Item.Quantity ) return TakeAll();

		var split = Item.Split( quantity );
		return split;
	}

	/// <summary>
	/// Take half (rounded up) from this slot — used for right-click pickup
	/// </summary>
	public ItemInstance TakeHalf()
	{
		if ( IsEmpty ) return null;
		int half = (Item.Quantity + 1) / 2;
		return Take( half );
	}

	/// <summary>
	/// Clear this slot
	/// </summary>
	public void Clear()
	{
		Item = null;
	}
}
