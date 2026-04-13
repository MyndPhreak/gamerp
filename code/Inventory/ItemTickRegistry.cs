using System;
using System.Collections.Generic;

namespace GameRP.Inventory;

/// <summary>
/// Registry of named tick handlers for items.
/// Items reference handlers by ID via ItemDefinition.TickHandlerId.
///
/// Register handlers in code:
///   ItemTickRegistry.Register("food.spoil", FoodSpoilHandler.OnTick);
/// </summary>
public static class ItemTickRegistry
{
	public delegate void TickHandler( ItemInstance item, ItemContainer container, int slotIndex, float delta );

	private static readonly Dictionary<string, TickHandler> _handlers = new();

	public static void Register( string id, TickHandler handler )
	{
		_handlers[id] = handler;
	}

	public static void Invoke( string id, ItemInstance item, ItemContainer container, int slotIndex, float delta )
	{
		if ( _handlers.TryGetValue( id, out var handler ) )
			handler?.Invoke( item, container, slotIndex, delta );
	}

	public static bool Has( string id ) => _handlers.ContainsKey( id );
}

/// <summary>
/// Built-in tick handlers for common item behaviors.
/// </summary>
public static class BuiltinTickHandlers
{
	public static void RegisterAll()
	{
		ItemTickRegistry.Register( "spoil", SpoilHandler );
		ItemTickRegistry.Register( "drain", DrainHandler );
		ItemTickRegistry.Register( "decay", DecayHandler );
	}

	/// <summary>
	/// Food spoilage. Increments a "freshness" NBT counter; when it hits the spoil threshold,
	/// the item is consumed (Quantity = 0).
	/// </summary>
	public static void SpoilHandler( ItemInstance item, ItemContainer container, int slotIndex, float delta )
	{
		float freshness = item.Nbt.GetNumber( "freshness", 100f );
		freshness -= delta * 0.5f; // 0.5 freshness per tick interval
		item.Nbt.SetNumber( "freshness", freshness );

		if ( freshness <= 0f )
		{
			item.Quantity = 0;
		}
	}

	/// <summary>
	/// Battery drain. Decrements an NBT "charge" value. When 0, item still exists but is "dead".
	/// </summary>
	public static void DrainHandler( ItemInstance item, ItemContainer container, int slotIndex, float delta )
	{
		float charge = item.Nbt.GetNumber( "charge", 100f );
		charge -= delta * 1f;
		if ( charge < 0f ) charge = 0f;
		item.Nbt.SetNumber( "charge", charge );
	}

	/// <summary>
	/// Durability decay over time
	/// </summary>
	public static void DecayHandler( ItemInstance item, ItemContainer container, int slotIndex, float delta )
	{
		if ( !item.GetDefinition().IsDamageable ) return;
		item.DamageItem( 1 );
		if ( item.GetCurrentDurability() <= 0 )
			item.Quantity = 0;
	}
}
