using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace GameRP.Inventory;

/// <summary>
/// Central registry of all item definitions in the game.
/// Register items here so they can be referenced by ID everywhere.
/// </summary>
public static class ItemRegistry
{
	private static readonly Dictionary<string, ItemDefinition> _items = new();

	static ItemRegistry()
	{
		RegisterDefaults();
	}

	public static void Register( ItemDefinition item )
	{
		_items[item.Id] = item;
	}

	public static ItemDefinition Get( string id )
	{
		return _items.TryGetValue( id, out var item ) ? item : null;
	}

	public static IEnumerable<ItemDefinition> GetAll()
	{
		return _items.Values;
	}

	public static IEnumerable<ItemDefinition> GetByCategory( string category )
	{
		return _items.Values.Where( i => i.Category == category );
	}

	public static IEnumerable<string> GetCategories()
	{
		return _items.Values.Select( i => i.Category ).Distinct().OrderBy( c => c );
	}

	private static void RegisterDefaults()
	{
		// ---- Food & Drink ----
		Register( new ItemDefinition { Id = "food.burger", Name = "Burger", Description = "A greasy burger.", Category = "Food", Icon = "fastfood", MaxStack = 10, BaseValue = 10, Consumable = true, Weight = 0.3f, Tags = "food,consumable" } );
		Register( new ItemDefinition { Id = "food.pizza", Name = "Pizza Slice", Description = "Pepperoni pizza slice.", Category = "Food", Icon = "local_dining", MaxStack = 10, BaseValue = 8, Consumable = true, Weight = 0.2f, Tags = "food,consumable" } );
		Register( new ItemDefinition { Id = "food.water", Name = "Water Bottle", Description = "Stay hydrated.", Category = "Food", Icon = "local_drink", MaxStack = 20, BaseValue = 3, Consumable = true, Weight = 0.5f, Tags = "food,drink,consumable" } );
		Register( new ItemDefinition { Id = "food.coffee", Name = "Coffee", Description = "Hot coffee.", Category = "Food", Icon = "local_cafe", MaxStack = 10, BaseValue = 5, Consumable = true, Weight = 0.3f, Tags = "food,drink,consumable" } );
		Register( new ItemDefinition { Id = "food.donut", Name = "Donut", Description = "A glazed donut.", Category = "Food", Icon = "cake", MaxStack = 12, BaseValue = 4, Consumable = true, Weight = 0.1f, Tags = "food,consumable" } );

		// ---- Tools ----
		Register( new ItemDefinition { Id = "tool.flashlight", Name = "Flashlight", Description = "Lights up dark areas.", Category = "Tools", Icon = "highlight", MaxStack = 1, BaseValue = 30, Weight = 0.5f, Tags = "tool,equipment" } );
		Register( new ItemDefinition { Id = "tool.phone", Name = "Phone", Description = "Call other players.", Category = "Tools", Icon = "smartphone", MaxStack = 1, BaseValue = 120, Weight = 0.2f, Tags = "tool,electronics" } );
		Register( new ItemDefinition { Id = "tool.radio", Name = "Radio", Description = "Communicate on radio channels.", Category = "Tools", Icon = "radio", MaxStack = 1, BaseValue = 90, Weight = 0.4f, Tags = "tool,electronics" } );
		Register( new ItemDefinition { Id = "tool.camera", Name = "Camera", Description = "Take photos.", Category = "Tools", Icon = "camera_alt", MaxStack = 1, BaseValue = 180, Weight = 0.3f, Tags = "tool,electronics" } );
		Register( new ItemDefinition { Id = "tool.lockpick", Name = "Lockpick Set", Description = "Opens locked doors.", Category = "Tools", Icon = "vpn_key", MaxStack = 5, BaseValue = 150, Consumable = true, Weight = 0.1f, Tags = "tool,illegal" } );

		// ---- Mining ----
		Register( new ItemDefinition { Id = "tool.pickaxe", Name = "Pickaxe", Description = "For mining ore.", Category = "Tools", Icon = "build", MaxStack = 1, BaseValue = 150, Weight = 3f, Tags = "tool,mining" } );
		Register( new ItemDefinition { Id = "tool.drill", Name = "Power Drill", Description = "Mines faster.", Category = "Tools", Icon = "settings", MaxStack = 1, BaseValue = 600, Weight = 5f, Tags = "tool,mining,power" } );

		// ---- Materials ----
		Register( new ItemDefinition { Id = "mat.wood", Name = "Wood Planks", Description = "Bundle of planks.", Category = "Materials", Icon = "view_module", MaxStack = 50, BaseValue = 10, Weight = 2f, Tags = "material,building" } );
		Register( new ItemDefinition { Id = "mat.metal", Name = "Metal Sheets", Description = "Sheets of metal.", Category = "Materials", Icon = "layers", MaxStack = 50, BaseValue = 25, Weight = 4f, Tags = "material,building" } );
		Register( new ItemDefinition { Id = "mat.gold_ore", Name = "Gold Ore", Description = "Raw gold ore. Sell to the Fed.", Category = "Materials", Icon = "star", MaxStack = 20, BaseValue = 200, Weight = 5f, Tags = "material,mining,valuable" } );
		Register( new ItemDefinition { Id = "mat.gold_bar", Name = "Gold Bar", Description = "Refined gold bar.", Category = "Materials", Icon = "monetization_on", MaxStack = 10, BaseValue = 1000, Weight = 8f, Tags = "material,valuable" } );

		// ---- Weapons ----
		Register( new ItemDefinition { Id = "weapon.bat", Name = "Baseball Bat", Description = "A wooden bat.", Category = "Weapons", Icon = "sports", MaxStack = 1, BaseValue = 60, Weight = 1.5f, Tags = "weapon,melee" } );
		Register( new ItemDefinition { Id = "weapon.knife", Name = "Knife", Description = "A sharp knife.", Category = "Weapons", Icon = "content_cut", MaxStack = 1, BaseValue = 45, Weight = 0.3f, Tags = "weapon,melee,illegal" } );
		Register( new ItemDefinition { Id = "weapon.pistol", Name = "Pistol", Description = "A 9mm handgun.", Category = "Weapons", Icon = "gps_fixed", MaxStack = 1, BaseValue = 300, Weight = 1f, Tags = "weapon,firearm,illegal" } );
		Register( new ItemDefinition { Id = "weapon.smg", Name = "SMG", Description = "Compact submachine gun.", Category = "Weapons", Icon = "my_location", MaxStack = 1, BaseValue = 1200, Weight = 2.5f, Tags = "weapon,firearm,illegal" } );
		Register( new ItemDefinition { Id = "weapon.shotgun", Name = "Shotgun", Description = "Pump-action shotgun.", Category = "Weapons", Icon = "adjust", MaxStack = 1, BaseValue = 900, Weight = 3.5f, Tags = "weapon,firearm,illegal" } );

		// ---- Ammo ----
		Register( new ItemDefinition { Id = "ammo.9mm", Name = "9mm Ammo", Description = "Box of 30 rounds.", Category = "Ammo", Icon = "lens", MaxStack = 200, BaseValue = 15, Consumable = true, Weight = 0.5f, Tags = "ammo" } );
		Register( new ItemDefinition { Id = "ammo.shell", Name = "Shotgun Shells", Description = "Box of 8 shells.", Category = "Ammo", Icon = "lens", MaxStack = 100, BaseValue = 20, Consumable = true, Weight = 0.6f, Tags = "ammo" } );
	}
}
