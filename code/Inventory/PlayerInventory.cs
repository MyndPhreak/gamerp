using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameRP.Inventory;

/// <summary>
/// Player inventory component. Holds two ItemContainers: a main storage container
/// (configurable slot count) and a hotbar (also configurable but typically 9).
///
/// Drives item tick events for all held items.
/// </summary>
[Category( "Inventory" )]
[Title( "Player Inventory" )]
[Icon( "backpack" )]
public sealed class PlayerInventory : Component
{
	[Property] public int InventorySlots { get; set; } = 27;
	[Property] public int HotbarSlots { get; set; } = 9;
	[Property] public float MaxWeight { get; set; } = 0f;

	/// <summary>
	/// Tick interval for inventory-level OnTick events (independent of per-item ticks)
	/// </summary>
	[Property] public float InventoryTickInterval { get; set; } = 1.0f;

	/// <summary>
	/// Main storage container (the "inventory" the player opens)
	/// </summary>
	public ItemContainer Main { get; private set; }

	/// <summary>
	/// Hotbar container — always-visible quick-access slots
	/// </summary>
	public ItemContainer Hotbar { get; private set; }

	/// <summary>
	/// Fired on a fixed interval (InventoryTickInterval). Delta is seconds since last tick.
	/// </summary>
	public Action<float> OnInventoryTick;

	/// <summary>
	/// Fired when a single item ticks (the item's TickInterval has elapsed).
	/// Args: item instance, the container it's in, the slot index, delta seconds.
	/// </summary>
	public Action<ItemInstance, ItemContainer, int, float> OnItemTick;

	/// <summary>
	/// Fired any time inventory contents change (either container)
	/// </summary>
	public Action OnChanged;

	private float _inventoryTickAcc;
	private float _itemTickAcc;
	private float _saveAcc;
	private const float ITEM_TICK_RATE = 0.25f; // check items 4x per second
	private const float SAVE_INTERVAL = 30f; // auto-save every 30 seconds

	private string _rowVersion = "";

	protected override void OnAwake()
	{
		Main = new ItemContainer( InventorySlots, "Inventory" ) { MaxWeight = MaxWeight };
		Hotbar = new ItemContainer( HotbarSlots, "Hotbar" );

		Main.OnContainerChanged += () => OnChanged?.Invoke();
		Hotbar.OnContainerChanged += () => OnChanged?.Invoke();
	}

	protected override void OnStart()
	{
		if ( !IsProxy )
		{
			LoadFromBackend();
		}
	}

	protected override void OnDestroy()
	{
		if ( !IsProxy )
		{
			_ = SaveToBackend();
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		var dt = Time.Delta;

		// Inventory tick
		_inventoryTickAcc += dt;
		if ( _inventoryTickAcc >= InventoryTickInterval )
		{
			OnInventoryTick?.Invoke( _inventoryTickAcc );
			_inventoryTickAcc = 0f;
		}

		// Item ticks (rate-limited check)
		_itemTickAcc += dt;
		if ( _itemTickAcc >= ITEM_TICK_RATE )
		{
			TickItems( _itemTickAcc );
			_itemTickAcc = 0f;
		}

		// Auto-save
		_saveAcc += dt;
		if ( _saveAcc >= SAVE_INTERVAL )
		{
			_saveAcc = 0f;
			_ = SaveToBackend();
		}
	}

	// ---- Backend persistence ----

	public async void LoadFromBackend()
	{
		try
		{
			Log.Info( $"[Inventory] Loading inventory for SteamId: {Game.SteamId}" );
			var dto = await InventorySystem.Api.GetInventoryAsync( Game.SteamId );
			if ( dto == null )
			{
				Log.Warning( "[Inventory] Failed to load from backend (null response)" );
				return;
			}

			Log.Info( $"[Inventory] Backend returned: MainSlots={dto.MainSlots}, HotbarSlots={dto.HotbarSlots}, Items={dto.Items?.Count ?? -1}" );

			// Only resize if the backend returned valid slot counts (> 0)
			if ( dto.MainSlots > 0 && dto.MainSlots != Main.SlotCount )
				Main.Resize( dto.MainSlots );
			if ( dto.HotbarSlots > 0 && dto.HotbarSlots != Hotbar.SlotCount )
				Hotbar.Resize( dto.HotbarSlots );

			// Clear and refill
			Main.Clear();
			Hotbar.Clear();

			if ( dto.Items != null )
			{
				foreach ( var itemDto in dto.Items )
				{
					var instance = new ItemInstance
					{
						InstanceId = itemDto.InstanceId,
						ItemId = itemDto.ItemId,
						Quantity = itemDto.Quantity,
						Durability = itemDto.Durability,
						Nbt = ItemNbt.Deserialize( itemDto.NbtJson )
					};

					var container = itemDto.Container == "hotbar" ? Hotbar : Main;
					var slot = container.GetSlot( itemDto.SlotIndex );
					if ( slot != null )
						slot.Item = instance;
				}
			}

			_rowVersion = dto.RowVersion ?? "";
			OnChanged?.Invoke();
			Log.Info( $"[Inventory] Loaded {dto.Items?.Count ?? 0} items. Slots: {Main.SlotCount} main, {Hotbar.SlotCount} hotbar" );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[Inventory] LoadFromBackend exception: {ex.Message}" );
		}
	}

	public async Task SaveToBackend()
	{
		try
		{
			var request = new SaveInventoryRequest
			{
				MainSlots = Main.SlotCount,
				HotbarSlots = Hotbar.SlotCount,
				SelectedHotbarSlot = Components.Get<HotbarSelector>()?.SelectedIndex ?? 0,
				RowVersion = _rowVersion,
				Items = new List<InventoryItemDto>()
			};

			SerializeContainer( Main, "main", request.Items );
			SerializeContainer( Hotbar, "hotbar", request.Items );

			var result = await InventorySystem.Api.SaveInventoryAsync( Game.SteamId, request );
			if ( result != null )
			{
				_rowVersion = result.RowVersion;
			}
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[Inventory] Save failed: {ex.Message}" );
		}
	}

	private void SerializeContainer( ItemContainer container, string name, List<InventoryItemDto> outList )
	{
		for ( int i = 0; i < container.SlotCount; i++ )
		{
			var slot = container.GetSlot( i );
			if ( slot == null || slot.IsEmpty ) continue;

			var item = slot.Item;
			outList.Add( new InventoryItemDto
			{
				InstanceId = item.InstanceId,
				Container = name,
				SlotIndex = i,
				ItemId = item.ItemId,
				Quantity = item.Quantity,
				Durability = item.Durability,
				NbtJson = item.Nbt?.Serialize() ?? "{}"
			} );
		}
	}

	private void TickItems( float delta )
	{
		TickContainerItems( Main, delta );
		TickContainerItems( Hotbar, delta );
	}

	private void TickContainerItems( ItemContainer container, float delta )
	{
		for ( int i = 0; i < container.SlotCount; i++ )
		{
			var slot = container.GetSlot( i );
			if ( slot == null || slot.IsEmpty ) continue;

			var item = slot.Item;
			var def = item.GetDefinition();
			if ( def == null || !def.IsTickable ) continue;

			item.TickAccumulator += delta;
			if ( item.TickAccumulator >= def.TickInterval )
			{
				float elapsed = item.TickAccumulator;
				item.TickAccumulator = 0f;

				// Run registered tick handler if any
				if ( !string.IsNullOrEmpty( def.TickHandlerId ) )
				{
					ItemTickRegistry.Invoke( def.TickHandlerId, item, container, i, elapsed );
				}

				OnItemTick?.Invoke( item, container, i, elapsed );

				// If the item was destroyed by the tick (quantity 0), clear the slot
				if ( item.Quantity <= 0 )
					slot.Clear();
			}
		}
	}

	// ---- Convenience methods ----

	/// <summary>
	/// Add an item to the player's inventory. Tries hotbar first if it has the same item type for stacking,
	/// then main inventory.
	/// </summary>
	public int Give( string itemId, int quantity = 1 )
	{
		var instance = new ItemInstance( itemId, quantity );

		// Try hotbar first if it has matching items to stack
		if ( Hotbar.Contains( itemId ) )
		{
			var leftover = Hotbar.TryAdd( instance );
			if ( leftover == null ) return quantity;
			instance = leftover;
		}

		// Then main inventory
		var rem = Main.TryAdd( instance );
		if ( rem == null ) return quantity;

		return quantity - rem.Quantity;
	}

	/// <summary>
	/// Add a pre-built item instance (preserves NBT)
	/// </summary>
	public ItemInstance GiveInstance( ItemInstance instance )
	{
		// Try hotbar first if it has matching
		if ( instance.GetDefinition()?.IsStackable == true && Hotbar.Contains( instance.ItemId ) )
		{
			var leftover = Hotbar.TryAdd( instance );
			if ( leftover == null ) return null;
			instance = leftover;
		}

		return Main.TryAdd( instance );
	}

	/// <summary>
	/// Remove a quantity of an item, checking both main and hotbar
	/// </summary>
	public int Take( string itemId, int quantity = 1 )
	{
		int remaining = quantity;

		// Hotbar first
		int removedHotbar = Hotbar.TryRemove( itemId, remaining );
		remaining -= removedHotbar;

		if ( remaining > 0 )
		{
			int removedMain = Main.TryRemove( itemId, remaining );
			remaining -= removedMain;
		}

		return quantity - remaining;
	}

	/// <summary>
	/// Total count across both containers
	/// </summary>
	public int CountItem( string itemId )
	{
		return Main.CountItem( itemId ) + Hotbar.CountItem( itemId );
	}

	public bool Has( string itemId, int quantity = 1 )
	{
		return CountItem( itemId ) >= quantity;
	}

	/// <summary>
	/// All items across both containers
	/// </summary>
	public IEnumerable<ItemInstance> AllItems()
	{
		foreach ( var item in Main.AllItems() ) yield return item;
		foreach ( var item in Hotbar.AllItems() ) yield return item;
	}

	public float TotalWeight => Main.TotalWeight + Hotbar.TotalWeight;
	public int UsedSlots => Main.UsedSlots + Hotbar.UsedSlots;
	public int TotalSlots => Main.SlotCount + Hotbar.SlotCount;
}
