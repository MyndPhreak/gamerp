using System;
using System.Collections.Generic;

namespace GameRP.Inventory;

/// <summary>
/// Context passed to item use handlers
/// </summary>
public class ItemUseContext
{
	public ItemInstance Item { get; set; }
	public ItemContainer Container { get; set; }
	public int SlotIndex { get; set; }
	public RPPlayer Player { get; set; }
	public bool Primary { get; set; } // true = left click, false = right click
}

/// <summary>
/// Registry of named use handlers for items.
/// Items reference handlers by ID via ItemDefinition.UseHandlerId.
/// </summary>
public static class ItemUseRegistry
{
	public delegate bool UseHandler( ItemUseContext ctx );

	private static readonly Dictionary<string, UseHandler> _handlers = new();

	public static void Register( string id, UseHandler handler )
	{
		_handlers[id] = handler;
	}

	public static bool Invoke( string id, ItemUseContext ctx )
	{
		if ( _handlers.TryGetValue( id, out var handler ) )
			return handler?.Invoke( ctx ) ?? false;
		return false;
	}

	public static bool Has( string id ) => _handlers.ContainsKey( id );
}

/// <summary>
/// Built-in use handlers for common item behaviors.
/// </summary>
public static class BuiltinUseHandlers
{
	public static void RegisterAll()
	{
		ItemUseRegistry.Register( "consume", ConsumeHandler );
		ItemUseRegistry.Register( "heal", HealHandler );
	}

	/// <summary>
	/// Generic consume — removes 1 from the stack
	/// </summary>
	public static bool ConsumeHandler( ItemUseContext ctx )
	{
		if ( ctx.Item == null || ctx.Item.Quantity <= 0 ) return false;
		ctx.Item.Quantity--;
		if ( ctx.Item.Quantity <= 0 )
			ctx.Container?.GetSlot( ctx.SlotIndex )?.Clear();
		return true;
	}

	/// <summary>
	/// Heal the player. Reads "heal" NBT number from item, defaults to 10.
	/// </summary>
	public static bool HealHandler( ItemUseContext ctx )
	{
		// Player health hookup goes here when health system exists
		return ConsumeHandler( ctx );
	}
}
