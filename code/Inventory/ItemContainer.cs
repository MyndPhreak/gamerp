using System;
using System.Collections.Generic;
using System.Linq;

namespace GameRP.Inventory;

/// <summary>
/// A container of item slots. Generic — used for player inventories, hotbars,
/// chests, vehicle storage, anything. Slot count is dynamic and set at construction.
///
/// Provides events for UI binding and a complete set of operations:
/// add, remove, move, swap, split, merge, sort, clear.
/// </summary>
public class ItemContainer
{
	/// <summary>
	/// All slots in this container (fixed size after construction)
	/// </summary>
	public ItemSlot[] Slots { get; private set; }

	/// <summary>
	/// Number of slots in this container
	/// </summary>
	public int SlotCount => Slots.Length;

	/// <summary>
	/// Optional name for debugging / UI display
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Maximum total weight (0 = unlimited)
	/// </summary>
	public float MaxWeight { get; set; } = 0f;

	// ---- Events ----

	/// <summary>
	/// Fired when a single slot changes
	/// </summary>
	public Action<int> OnSlotChanged;

	/// <summary>
	/// Fired when ANY change happens to the container (add/remove/move)
	/// </summary>
	public Action OnContainerChanged;

	/// <summary>
	/// Fired when an item is added (item, quantity, slot index)
	/// </summary>
	public Action<ItemInstance, int, int> OnItemAdded;

	/// <summary>
	/// Fired when an item is removed (item ID, quantity, slot index)
	/// </summary>
	public Action<string, int, int> OnItemRemoved;

	public ItemContainer( int slotCount, string name = "Container" )
	{
		Name = name;
		Slots = new ItemSlot[slotCount];
		for ( int i = 0; i < slotCount; i++ )
		{
			Slots[i] = new ItemSlot { Index = i };
		}
	}

	/// <summary>
	/// Resize the container. Items in slots beyond the new count are dropped to a return list.
	/// </summary>
	public List<ItemInstance> Resize( int newSize )
	{
		var dropped = new List<ItemInstance>();
		var newSlots = new ItemSlot[newSize];

		for ( int i = 0; i < newSize; i++ )
		{
			if ( i < Slots.Length )
			{
				newSlots[i] = Slots[i];
				newSlots[i].Index = i;
			}
			else
			{
				newSlots[i] = new ItemSlot { Index = i };
			}
		}

		// Items that don't fit
		for ( int i = newSize; i < Slots.Length; i++ )
		{
			if ( !Slots[i].IsEmpty )
				dropped.Add( Slots[i].Item );
		}

		Slots = newSlots;
		Notify();
		return dropped;
	}

	// ---- Queries ----

	public ItemSlot GetSlot( int index )
	{
		if ( index < 0 || index >= Slots.Length ) return null;
		return Slots[index];
	}

	public IEnumerable<ItemSlot> NonEmptySlots()
	{
		foreach ( var slot in Slots )
			if ( !slot.IsEmpty ) yield return slot;
	}

	public IEnumerable<ItemInstance> AllItems()
	{
		foreach ( var slot in Slots )
			if ( !slot.IsEmpty ) yield return slot.Item;
	}

	public int CountItem( string itemId )
	{
		int count = 0;
		foreach ( var slot in Slots )
			if ( !slot.IsEmpty && slot.Item.ItemId == itemId )
				count += slot.Item.Quantity;
		return count;
	}

	public bool Contains( string itemId, int quantity = 1 )
	{
		return CountItem( itemId ) >= quantity;
	}

	public int FirstEmptyIndex()
	{
		for ( int i = 0; i < Slots.Length; i++ )
			if ( Slots[i].IsEmpty ) return i;
		return -1;
	}

	public int FirstSlotWith( string itemId )
	{
		for ( int i = 0; i < Slots.Length; i++ )
			if ( !Slots[i].IsEmpty && Slots[i].Item.ItemId == itemId ) return i;
		return -1;
	}

	public float TotalWeight
	{
		get
		{
			float w = 0f;
			foreach ( var slot in Slots )
			{
				if ( slot.IsEmpty ) continue;
				var def = slot.Item.GetDefinition();
				if ( def != null ) w += def.Weight * slot.Item.Quantity;
			}
			return w;
		}
	}

	public int UsedSlots
	{
		get
		{
			int n = 0;
			foreach ( var slot in Slots )
				if ( !slot.IsEmpty ) n++;
			return n;
		}
	}

	public int FreeSlots => SlotCount - UsedSlots;

	// ---- Add ----

	/// <summary>
	/// Try to add an item. Stacks onto existing matching slots first, then fills empty slots.
	/// Returns the leftover ItemInstance (or null if fully added).
	/// </summary>
	public ItemInstance TryAdd( ItemInstance item )
	{
		if ( item == null || item.Quantity <= 0 ) return null;

		var def = item.GetDefinition();
		if ( def == null ) return item;

		// Weight check
		if ( MaxWeight > 0 )
		{
			float incomingWeight = def.Weight * item.Quantity;
			if ( TotalWeight + incomingWeight > MaxWeight )
				return item; // refuse for now (could partial)
		}

		// First pass: stack onto existing slots
		if ( def.IsStackable )
		{
			for ( int i = 0; i < Slots.Length; i++ )
			{
				var slot = Slots[i];
				if ( slot.IsEmpty ) continue;
				if ( !slot.Item.CanStackWith( item ) ) continue;

				int space = def.MaxStack - slot.Item.Quantity;
				if ( space <= 0 ) continue;

				int toMove = Math.Min( space, item.Quantity );
				slot.Item.Quantity += toMove;
				item.Quantity -= toMove;

				NotifySlot( i );
				OnItemAdded?.Invoke( slot.Item, toMove, i );

				if ( item.Quantity <= 0 )
				{
					Notify();
					return null;
				}
			}
		}

		// Second pass: place in empty slots
		while ( item.Quantity > 0 )
		{
			int idx = FirstEmptyIndex();
			if ( idx == -1 ) break;

			var slot = Slots[idx];
			if ( !slot.CanAccept( item ) )
			{
				// Filtered slot, skip — find next empty after this
				int next = -1;
				for ( int i = idx + 1; i < Slots.Length; i++ )
					if ( Slots[i].IsEmpty ) { next = i; break; }
				if ( next == -1 ) break;
				idx = next;
				slot = Slots[idx];
				if ( !slot.CanAccept( item ) ) break;
			}

			if ( def.IsStackable && item.Quantity > def.MaxStack )
			{
				// Split the stack
				var placed = item.Split( def.MaxStack );
				slot.Item = placed;
			}
			else
			{
				slot.Item = item.Clone();
				item.Quantity = 0;
			}

			NotifySlot( idx );
			OnItemAdded?.Invoke( slot.Item, slot.Item.Quantity, idx );
		}

		Notify();

		if ( item.Quantity <= 0 ) return null;
		return item;
	}

	/// <summary>
	/// Convenience: add a quantity by item ID
	/// </summary>
	public int TryAdd( string itemId, int quantity = 1 )
	{
		var instance = new ItemInstance( itemId, quantity );
		var leftover = TryAdd( instance );
		return quantity - (leftover?.Quantity ?? 0);
	}

	// ---- Remove ----

	/// <summary>
	/// Remove a quantity of an item by ID. Returns how many were actually removed.
	/// Removes from smallest stacks first.
	/// </summary>
	public int TryRemove( string itemId, int quantity = 1 )
	{
		int remaining = quantity;

		for ( int i = Slots.Length - 1; i >= 0 && remaining > 0; i-- )
		{
			var slot = Slots[i];
			if ( slot.IsEmpty || slot.Item.ItemId != itemId ) continue;

			int toRemove = Math.Min( remaining, slot.Item.Quantity );
			slot.Item.Quantity -= toRemove;
			remaining -= toRemove;

			OnItemRemoved?.Invoke( itemId, toRemove, i );

			if ( slot.Item.Quantity <= 0 )
				slot.Clear();

			NotifySlot( i );
		}

		int removed = quantity - remaining;
		if ( removed > 0 ) Notify();
		return removed;
	}

	/// <summary>
	/// Remove a specific instance by ID
	/// </summary>
	public bool TryRemoveInstance( string instanceId )
	{
		for ( int i = 0; i < Slots.Length; i++ )
		{
			if ( !Slots[i].IsEmpty && Slots[i].Item.InstanceId == instanceId )
			{
				var item = Slots[i].Item;
				Slots[i].Clear();
				OnItemRemoved?.Invoke( item.ItemId, item.Quantity, i );
				NotifySlot( i );
				Notify();
				return true;
			}
		}
		return false;
	}

	// ---- Slot operations (drag/drop) ----

	/// <summary>
	/// Move an item from one slot to another within the same container.
	/// Handles merging, swapping, and splitting.
	/// </summary>
	public bool MoveSlot( int fromIndex, int toIndex )
	{
		if ( fromIndex == toIndex ) return false;
		var from = GetSlot( fromIndex );
		var to = GetSlot( toIndex );
		if ( from == null || to == null ) return false;
		if ( from.IsEmpty ) return false;
		if ( !to.CanAccept( from.Item ) ) return false;

		// Empty target — just move
		if ( to.IsEmpty )
		{
			to.Item = from.Item;
			from.Item = null;
		}
		// Same item — merge
		else if ( to.Item.CanStackWith( from.Item ) )
		{
			var def = to.Item.GetDefinition();
			int space = def.MaxStack - to.Item.Quantity;
			int moved = Math.Min( space, from.Item.Quantity );

			to.Item.Quantity += moved;
			from.Item.Quantity -= moved;

			if ( from.Item.Quantity <= 0 ) from.Clear();
		}
		// Different items — swap (if from-slot also accepts target item)
		else if ( from.CanAccept( to.Item ) )
		{
			var temp = from.Item;
			from.Item = to.Item;
			to.Item = temp;
		}
		else
		{
			return false;
		}

		NotifySlot( fromIndex );
		NotifySlot( toIndex );
		Notify();
		return true;
	}

	/// <summary>
	/// Move an item between two different containers
	/// </summary>
	public static bool TransferSlot( ItemContainer fromContainer, int fromIndex, ItemContainer toContainer, int toIndex )
	{
		var from = fromContainer.GetSlot( fromIndex );
		var to = toContainer.GetSlot( toIndex );
		if ( from == null || to == null ) return false;
		if ( from.IsEmpty ) return false;
		if ( !to.CanAccept( from.Item ) ) return false;

		if ( to.IsEmpty )
		{
			to.Item = from.Item;
			from.Item = null;
		}
		else if ( to.Item.CanStackWith( from.Item ) )
		{
			var def = to.Item.GetDefinition();
			int space = def.MaxStack - to.Item.Quantity;
			int moved = Math.Min( space, from.Item.Quantity );
			to.Item.Quantity += moved;
			from.Item.Quantity -= moved;
			if ( from.Item.Quantity <= 0 ) from.Clear();
		}
		else if ( from.CanAccept( to.Item ) )
		{
			var temp = from.Item;
			from.Item = to.Item;
			to.Item = temp;
		}
		else return false;

		fromContainer.NotifySlot( fromIndex );
		toContainer.NotifySlot( toIndex );
		fromContainer.Notify();
		toContainer.Notify();
		return true;
	}

	/// <summary>
	/// Quick-move (shift-click): try to push the slot's contents into another container
	/// in the most natural way (stack first, then fill empty slots).
	/// </summary>
	public bool QuickMoveTo( int fromIndex, ItemContainer target )
	{
		var from = GetSlot( fromIndex );
		if ( from == null || from.IsEmpty ) return false;

		var leftover = target.TryAdd( from.Item );
		if ( leftover == null )
		{
			from.Clear();
			NotifySlot( fromIndex );
			Notify();
			return true;
		}

		// Some items moved, leftover stays
		from.Item = leftover.Quantity > 0 ? leftover : null;
		NotifySlot( fromIndex );
		Notify();
		return leftover.Quantity == 0;
	}

	/// <summary>
	/// Sort the container — empty slots last, items grouped by ID, stacks consolidated.
	/// </summary>
	public void Sort()
	{
		// Collect all items
		var allItems = new List<ItemInstance>();
		foreach ( var slot in Slots )
		{
			if ( slot.Locked ) continue;
			if ( !slot.IsEmpty ) allItems.Add( slot.Item );
		}

		// Clear unlocked slots
		foreach ( var slot in Slots )
			if ( !slot.Locked ) slot.Clear();

		// Group and consolidate
		var grouped = allItems
			.GroupBy( i => i.ItemId )
			.OrderBy( g => g.First().GetDefinition()?.Category ?? "zzz" )
			.ThenBy( g => g.Key );

		foreach ( var group in grouped )
		{
			foreach ( var item in group )
			{
				TryAdd( item );
			}
		}

		Notify();
	}

	/// <summary>
	/// Clear all items
	/// </summary>
	public void Clear()
	{
		for ( int i = 0; i < Slots.Length; i++ )
		{
			if ( Slots[i].Locked ) continue;
			Slots[i].Clear();
			NotifySlot( i );
		}
		Notify();
	}

	// ---- Internal ----

	public void NotifySlotChanged( int index )
	{
		OnSlotChanged?.Invoke( index );
		OnContainerChanged?.Invoke();
	}

	private void NotifySlot( int index )
	{
		OnSlotChanged?.Invoke( index );
	}

	private void Notify()
	{
		OnContainerChanged?.Invoke();
	}
}
