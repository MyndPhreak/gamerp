using Sandbox;
using GameRP.Interactions;
using GameRP.Inventory;
using GameRP.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameRP.Shops;

public enum OwnerType { NPC, Player }

/// <summary>
/// Main shop component. Add alongside an Interactable to create a shop.
/// Configure listings directly in the editor — each listing references an ItemRegistry ID.
/// Works for NPC shops (fixed inventory) and player-owned shops (earnings go to owner).
/// </summary>
[Category( "Shops" )]
[Title( "Shop Keeper" )]
[Icon( "storefront" )]
public sealed class ShopKeeper : Component
{
	[Property, Group( "Info" )] public string ShopName { get; set; } = "Shop";
	[Property, Group( "Info" )] public string ShopDescription { get; set; } = "";
	[Property, Group( "Info" )] public OwnerType Owner { get; set; } = OwnerType.NPC;
	[Property, Group( "Info" )] public long OwnerSteamId { get; set; }

	[Property, Group( "Pricing" ), Range( 0f, 0.3f )] public float TaxRate { get; set; } = 0.07f;

	/// <summary>
	/// The items this shop sells/buys. Configure in the editor.
	/// Each entry is an ItemRegistry ID + buy price + sell price + stock.
	/// </summary>
	[Property, Group( "Inventory" )] public List<ShopListing> Listings { get; set; } = new();

	/// <summary>
	/// Fired after a successful purchase
	/// </summary>
	public Action<string, int, long> OnPurchase;

	/// <summary>
	/// Fired after a successful sale
	/// </summary>
	public Action<string, int, long> OnSale;

	private Interactable _interactable;

	protected override void OnStart()
	{
		_interactable = Components.Get<Interactable>();
		if ( _interactable != null )
		{
			_interactable.OnInteract = OnShopInteract;
			_interactable.InteractionText = $"Press E — {ShopName}";
		}
	}

	private void OnShopInteract()
	{
		var shopScreen = Scene.GetAllComponents<GameRP.UI.ShopScreen>().FirstOrDefault();
		if ( shopScreen != null )
		{
			shopScreen.Open( this );
		}
		else
		{
			Log.Error( "[Shop] No ShopScreen found in scene! Add one to the HUD GameObject." );
		}
	}

	/// <summary>
	/// Get all listings that can be bought
	/// </summary>
	public IEnumerable<ShopListing> GetBuyableListings()
	{
		return Listings.Where( l => l.CanBuy );
	}

	/// <summary>
	/// Get all listings where the shop will buy from players
	/// </summary>
	public IEnumerable<ShopListing> GetSellableListings()
	{
		return Listings.Where( l => l.CanSell );
	}

	/// <summary>
	/// Get all unique categories from the listings
	/// </summary>
	public IEnumerable<string> GetCategories( bool buying )
	{
		var listings = buying ? GetBuyableListings() : GetSellableListings();
		return listings
			.Select( l => ItemRegistry.Get( l.ItemId )?.Category ?? "Other" )
			.Distinct()
			.OrderBy( c => c );
	}

	/// <summary>
	/// Final price including tax
	/// </summary>
	public int GetPriceWithTax( int basePrice )
	{
		return (int)( basePrice * (1f + TaxRate) );
	}

	/// <summary>
	/// Get a mutable reference to a listing by item ID
	/// </summary>
	public int FindListingIndex( string itemId )
	{
		for ( int i = 0; i < Listings.Count; i++ )
		{
			if ( Listings[i].ItemId == itemId )
				return i;
		}
		return -1;
	}

	/// <summary>
	/// Process a purchase: player buys item from shop.
	/// Deducts money, adds to player inventory, adjusts stock.
	/// </summary>
	public async Task<bool> TryBuyItem( string itemId, long buyerSteamId, PlayerInventory buyerInventory )
	{
		int idx = FindListingIndex( itemId );
		if ( idx == -1 ) return false;

		var listing = Listings[idx];
		if ( !listing.CanBuy ) return false;

		int totalCost = GetPriceWithTax( listing.BuyPrice );

		// Check if inventory can hold the item
		var def = ItemRegistry.Get( itemId );
		if ( def == null ) return false;

		if ( !def.IsStackable && !buyerInventory.Has( itemId ) && buyerInventory.UsedSlots >= buyerInventory.TotalSlots )
		{
			Log.Warning( "[Shop] Inventory full" );
			return false;
		}

		// Withdraw from buyer
		var result = await EconomySystem.Withdraw( buyerSteamId, totalCost, $"Shop: {def.Name} from {ShopName}" );
		if ( result == null ) return false;

		// Add to inventory
		buyerInventory.Give( itemId, 1 );

		// Deduct stock
		if ( listing.Stock > 0 )
		{
			listing.Stock--;
			Listings[idx] = listing;
		}

		// Pay shop owner if player-owned
		if ( Owner == OwnerType.Player && OwnerSteamId > 0 )
		{
			await EconomySystem.Deposit( OwnerSteamId, listing.BuyPrice, $"Shop sale: {def.Name}" );
		}

		OnPurchase?.Invoke( itemId, totalCost, buyerSteamId );
		Log.Info( $"[Shop] {buyerSteamId} bought {def.Name} for ${totalCost}" );
		return true;
	}

	/// <summary>
	/// Process a sale: player sells item to shop.
	/// Adds money, removes from player inventory, adjusts stock.
	/// </summary>
	public async Task<bool> TrySellItem( string itemId, long sellerSteamId, PlayerInventory sellerInventory )
	{
		int idx = FindListingIndex( itemId );
		if ( idx == -1 ) return false;

		var listing = Listings[idx];
		if ( !listing.CanSell ) return false;

		// Check player actually has the item
		if ( !sellerInventory.Has( itemId ) ) return false;

		// Deposit to seller
		var result = await EconomySystem.Deposit( sellerSteamId, listing.SellPrice, $"Sold: {ItemRegistry.Get( itemId )?.Name} to {ShopName}" );
		if ( result == null ) return false;

		// Remove from inventory
		sellerInventory.Take( itemId, 1 );

		// Add stock back
		if ( listing.Stock >= 0 )
		{
			listing.Stock++;
			Listings[idx] = listing;
		}

		// Deduct from owner if player-owned
		if ( Owner == OwnerType.Player && OwnerSteamId > 0 )
		{
			await EconomySystem.Withdraw( OwnerSteamId, listing.SellPrice, $"Shop buyback: {ItemRegistry.Get( itemId )?.Name}" );
		}

		OnSale?.Invoke( itemId, listing.SellPrice, sellerSteamId );
		return true;
	}
}
