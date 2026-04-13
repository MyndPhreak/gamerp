using Sandbox;

namespace GameRP.Shops;

/// <summary>
/// A single listing in a shop — what item, at what price, how much stock.
/// This is a struct so it can be configured as a [Property] list in the editor.
/// </summary>
public struct ShopListing
{
	/// <summary>
	/// Item ID from the ItemRegistry (e.g. "food.burger", "weapon.pistol")
	/// </summary>
	[Property, Title( "Item ID" )] public string ItemId { get; set; }

	/// <summary>
	/// Override display name (leave empty to use ItemDefinition name)
	/// </summary>
	[Property] public string NameOverride { get; set; }

	/// <summary>
	/// Price to buy from this shop. 0 = not for sale.
	/// </summary>
	[Property] public int BuyPrice { get; set; }

	/// <summary>
	/// Price the shop pays when a player sells this item. 0 = won't buy.
	/// </summary>
	[Property] public int SellPrice { get; set; }

	/// <summary>
	/// Current stock. -1 = unlimited.
	/// </summary>
	[Property] public int Stock { get; set; }

	/// <summary>
	/// Max stock (for restocking). -1 = unlimited.
	/// </summary>
	[Property] public int MaxStock { get; set; }

	public ShopListing()
	{
		ItemId = "";
		NameOverride = "";
		BuyPrice = 0;
		SellPrice = 0;
		Stock = -1;
		MaxStock = -1;
	}

	public bool CanBuy => BuyPrice > 0 && Stock != 0;
	public bool CanSell => SellPrice > 0 && (MaxStock == -1 || Stock < MaxStock);
}
