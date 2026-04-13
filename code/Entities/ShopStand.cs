using Sandbox;
using GameRP.Interactions;
using GameRP.Shops;
using System.Linq;

namespace GameRP.Entities;

/// <summary>
/// A physical shop in the world. Requires sibling Interactable + ShopKeeper components.
/// Optionally enforces business hours using DayNightCycle.
///
/// Setup:
/// 1. Create GameObject with a model (counter, stall, etc.)
/// 2. Add Interactable — set distance/prompt
/// 3. Add ShopKeeper — set name, add listings (item IDs + prices + stock)
/// 4. Optionally add this ShopStand for business hours / NPC visuals
/// </summary>
[Category( "Entities" )]
[Title( "Shop Stand" )]
[Icon( "storefront" )]
public sealed class ShopStand : Component
{
	[Property] public bool UseBusinessHours { get; set; } = false;
	[Property, Range( 0f, 24f )] public float OpenHour { get; set; } = 6f;
	[Property, Range( 0f, 24f )] public float CloseHour { get; set; } = 22f;

	private ShopKeeper _shopKeeper;
	private Interactable _interactable;
	private bool _isOpen = true;

	protected override void OnStart()
	{
		_shopKeeper = Components.Get<ShopKeeper>();
		_interactable = Components.Get<Interactable>();
	}

	protected override void OnUpdate()
	{
		if ( !UseBusinessHours ) return;

		var dayNight = Scene.GetAllComponents<DayNightCycle>().FirstOrDefault();
		if ( dayNight == null ) return;

		bool shouldBeOpen;
		if ( OpenHour < CloseHour )
			shouldBeOpen = dayNight.TimeOfDay >= OpenHour && dayNight.TimeOfDay < CloseHour;
		else
			shouldBeOpen = dayNight.TimeOfDay >= OpenHour || dayNight.TimeOfDay < CloseHour;

		if ( shouldBeOpen == _isOpen ) return;

		_isOpen = shouldBeOpen;

		if ( _interactable != null )
		{
			_interactable.InteractionText = _isOpen
				? $"Press E — {_shopKeeper?.ShopName ?? "Shop"}"
				: $"{_shopKeeper?.ShopName ?? "Shop"} — CLOSED";
		}
	}
}
