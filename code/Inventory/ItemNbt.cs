using Sandbox;
using System.Collections.Generic;

namespace GameRP.Inventory;

/// <summary>
/// Strongly-typed per-instance item data (Named Binary Tag — Minecraft term).
/// Holds custom names, lore, modifiers, and arbitrary key/value data.
/// JSON-serializable for backend persistence.
/// </summary>
public class ItemNbt
{
	/// <summary>
	/// Override display name (e.g. a player-renamed sword)
	/// </summary>
	public string CustomName { get; set; }

	/// <summary>
	/// Extra description lines shown in tooltip below the base description
	/// </summary>
	public List<string> Lore { get; set; } = new();

	/// <summary>
	/// Modifiers/enchantments applied to this instance
	/// </summary>
	public List<ItemModifier> Modifiers { get; set; } = new();

	/// <summary>
	/// Arbitrary string key/value data for game-specific needs
	/// (ammo count, charge level, contained liquid, serial number, etc.)
	/// </summary>
	public Dictionary<string, string> Data { get; set; } = new();

	/// <summary>
	/// Numeric data for things like ammo counts, charges, fluid amounts
	/// </summary>
	public Dictionary<string, float> Numbers { get; set; } = new();

	public string GetString( string key, string defaultValue = "" )
		=> Data.TryGetValue( key, out var v ) ? v : defaultValue;

	public float GetNumber( string key, float defaultValue = 0f )
		=> Numbers.TryGetValue( key, out var v ) ? v : defaultValue;

	public void SetString( string key, string value ) => Data[key] = value;
	public void SetNumber( string key, float value ) => Numbers[key] = value;

	public bool HasModifier( string id )
	{
		foreach ( var mod in Modifiers )
			if ( mod.Id == id ) return true;
		return false;
	}

	public void AddModifier( string id, int level = 1 )
	{
		for ( int i = 0; i < Modifiers.Count; i++ )
		{
			if ( Modifiers[i].Id == id )
			{
				Modifiers[i] = new ItemModifier { Id = id, Level = level };
				return;
			}
		}
		Modifiers.Add( new ItemModifier { Id = id, Level = level } );
	}

	public string Serialize()
	{
		return Json.Serialize( this );
	}

	public static ItemNbt Deserialize( string json )
	{
		if ( string.IsNullOrEmpty( json ) ) return new ItemNbt();
		try { return Json.Deserialize<ItemNbt>( json ) ?? new ItemNbt(); }
		catch { return new ItemNbt(); }
	}
}

public struct ItemModifier
{
	public string Id { get; set; }
	public int Level { get; set; }
}
