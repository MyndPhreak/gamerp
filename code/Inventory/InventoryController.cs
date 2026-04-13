using Sandbox;
using System;

namespace GameRP.Inventory;

/// <summary>
/// Singleton-style controller that holds the currently dragged item and tracks
/// the source slot. UI panels read from this to render the drag ghost and
/// resolve drop targets.
/// </summary>
[Category( "Inventory" )]
[Title( "Inventory Controller" )]
[Icon( "drag_indicator" )]
public sealed class InventoryController : Component
{
	/// <summary>
	/// The item the player is currently holding with the cursor (drag-drop)
	/// </summary>
	public ItemInstance HeldItem { get; private set; }

	/// <summary>
	/// The container the held item came from
	/// </summary>
	public ItemContainer SourceContainer { get; private set; }

	/// <summary>
	/// The slot index the held item came from
	/// </summary>
	public int SourceSlotIndex { get; private set; } = -1;

	/// <summary>
	/// True while a drag is in progress
	/// </summary>
	public bool IsDragging => HeldItem != null;

	/// <summary>
	/// Fired when held state changes (drag start, drop)
	/// </summary>
	public Action OnHeldChanged;

	/// <summary>
	/// Pick up an item from a slot. Optionally take only half (right-click).
	/// </summary>
	public void PickUp( ItemContainer container, int slotIndex, bool half = false )
	{
		if ( IsDragging ) return;

		var slot = container?.GetSlot( slotIndex );
		if ( slot == null || slot.IsEmpty ) return;

		HeldItem = half ? slot.TakeHalf() : slot.TakeAll();
		SourceContainer = container;
		SourceSlotIndex = slotIndex;

		OnHeldChanged?.Invoke();
	}

	/// <summary>
	/// Drop the held item into a target slot. Handles merge / swap / split.
	/// `placeOne` = right-click to place a single item from the held stack.
	/// </summary>
	public void Drop( ItemContainer container, int slotIndex, bool placeOne = false )
	{
		if ( !IsDragging ) return;
		if ( container == null ) return;

		var slot = container.GetSlot( slotIndex );
		if ( slot == null ) return;
		if ( !slot.CanAccept( HeldItem ) )
		{
			ReturnToSource();
			return;
		}

		// Place one (right-click drop)
		if ( placeOne && HeldItem.Quantity > 1 )
		{
			var single = HeldItem.Clone();
			single.Quantity = 1;

			var leftover = slot.MergeOrPlace( single );
			if ( leftover == null )
			{
				HeldItem.Quantity--;
				if ( HeldItem.Quantity <= 0 )
				{
					HeldItem = null;
					SourceContainer = null;
					SourceSlotIndex = -1;
				}
				container.NotifySlotChanged( slotIndex );
				OnHeldChanged?.Invoke();
				return;
			}
		}

		// Empty target → place all
		if ( slot.IsEmpty )
		{
			slot.Item = HeldItem;
			HeldItem = null;
			SourceContainer = null;
			SourceSlotIndex = -1;
			container.NotifySlotChanged( slotIndex );
			OnHeldChanged?.Invoke();
			return;
		}

		// Same item → merge
		if ( slot.Item.CanStackWith( HeldItem ) )
		{
			var def = slot.Item.GetDefinition();
			int space = def.MaxStack - slot.Item.Quantity;
			int moved = Math.Min( space, HeldItem.Quantity );

			slot.Item.Quantity += moved;
			HeldItem.Quantity -= moved;

			if ( HeldItem.Quantity <= 0 )
			{
				HeldItem = null;
				SourceContainer = null;
				SourceSlotIndex = -1;
			}

			container.NotifySlotChanged( slotIndex );
			OnHeldChanged?.Invoke();
			return;
		}

		// Different items → swap
		var swap = slot.Item;
		slot.Item = HeldItem;
		HeldItem = swap;
		// Source stays for potential return
		container.NotifySlotChanged( slotIndex );
		OnHeldChanged?.Invoke();
	}

	/// <summary>
	/// Right-click on a slot — picks up half if not dragging, places one if dragging.
	/// </summary>
	public void RightClick( ItemContainer container, int slotIndex )
	{
		if ( IsDragging )
		{
			Drop( container, slotIndex, placeOne: true );
		}
		else
		{
			PickUp( container, slotIndex, half: true );
		}
	}

	/// <summary>
	/// Cancel the current drag and return the item to its source slot.
	/// </summary>
	public void ReturnToSource()
	{
		if ( !IsDragging ) return;
		if ( SourceContainer != null && SourceSlotIndex >= 0 )
		{
			var slot = SourceContainer.GetSlot( SourceSlotIndex );
			if ( slot != null )
			{
				var leftover = slot.MergeOrPlace( HeldItem );
				if ( leftover != null )
				{
					// Couldn't return — just drop it (TODO: spawn world item)
					Log.Warning( "[Inventory] Could not return held item to source" );
				}
			}
		}

		HeldItem = null;
		SourceContainer = null;
		SourceSlotIndex = -1;
		OnHeldChanged?.Invoke();
	}

	/// <summary>
	/// Force-clear the held item without returning (for shift-click moves)
	/// </summary>
	public void Clear()
	{
		HeldItem = null;
		SourceContainer = null;
		SourceSlotIndex = -1;
		OnHeldChanged?.Invoke();
	}
}
