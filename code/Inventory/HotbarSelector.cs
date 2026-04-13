using Sandbox;
using System;

namespace GameRP.Inventory;

/// <summary>
/// Tracks which hotbar slot the player has selected.
/// Reads number key inputs (1-9) and scroll wheel.
/// </summary>
[Category( "Inventory" )]
[Title( "Hotbar Selector" )]
[Icon( "view_carousel" )]
public sealed class HotbarSelector : Component
{
	[Property] public PlayerInventory Inventory { get; set; }

	/// <summary>
	/// Currently selected hotbar slot index (0-based)
	/// </summary>
	public int SelectedIndex { get; private set; } = 0;

	/// <summary>
	/// Currently selected item (or null if slot is empty)
	/// </summary>
	public ItemInstance SelectedItem
	{
		get
		{
			if ( Inventory == null || Inventory.Hotbar == null ) return null;
			var slot = Inventory.Hotbar.GetSlot( SelectedIndex );
			return slot?.Item;
		}
	}

	/// <summary>
	/// Fired when the selected slot changes (oldIndex, newIndex)
	/// </summary>
	public Action<int, int> OnSelectionChanged;

	protected override void OnAwake()
	{
		if ( Inventory == null )
			Inventory = Components.Get<PlayerInventory>();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( Inventory == null || Inventory.Hotbar == null ) return;

		int slotCount = Inventory.Hotbar.SlotCount;
		int newIndex = SelectedIndex;

		// Number keys 1-9
		for ( int i = 0; i < Math.Min( 9, slotCount ); i++ )
		{
			if ( Input.Pressed( $"Slot{i + 1}" ) )
			{
				newIndex = i;
				break;
			}
		}

		// Scroll wheel
		var scroll = Input.MouseWheel.y;
		if ( scroll != 0 )
		{
			if ( scroll > 0 ) newIndex = (newIndex - 1 + slotCount) % slotCount;
			else newIndex = (newIndex + 1) % slotCount;
		}

		if ( newIndex != SelectedIndex )
		{
			int old = SelectedIndex;
			SelectedIndex = newIndex;
			OnSelectionChanged?.Invoke( old, newIndex );
		}
	}

	/// <summary>
	/// Set the selected slot programmatically
	/// </summary>
	public void Select( int index )
	{
		if ( Inventory?.Hotbar == null ) return;
		if ( index < 0 || index >= Inventory.Hotbar.SlotCount ) return;
		if ( index == SelectedIndex ) return;

		int old = SelectedIndex;
		SelectedIndex = index;
		OnSelectionChanged?.Invoke( old, index );
	}
}
