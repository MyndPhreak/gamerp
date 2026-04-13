using Sandbox;

namespace GameRP.Inventory;

/// <summary>
/// Item rarity tiers. Affects tooltip color and price modifiers.
/// </summary>
public enum ItemRarity
{
	Common,
	Uncommon,
	Rare,
	Epic,
	Legendary,
	Mythic
}

public static class ItemRarityExtensions
{
	public static Color GetColor( this ItemRarity rarity )
	{
		return rarity switch
		{
			ItemRarity.Common => new Color( 0.85f, 0.85f, 0.85f ),
			ItemRarity.Uncommon => new Color( 0.30f, 0.85f, 0.30f ),
			ItemRarity.Rare => new Color( 0.30f, 0.55f, 0.95f ),
			ItemRarity.Epic => new Color( 0.65f, 0.30f, 0.95f ),
			ItemRarity.Legendary => new Color( 0.95f, 0.65f, 0.20f ),
			ItemRarity.Mythic => new Color( 0.95f, 0.30f, 0.55f ),
			_ => Color.White
		};
	}

	public static string GetHex( this ItemRarity rarity )
	{
		return rarity switch
		{
			ItemRarity.Common => "#d9d9d9",
			ItemRarity.Uncommon => "#4cdb4c",
			ItemRarity.Rare => "#4d8cf2",
			ItemRarity.Epic => "#a64df2",
			ItemRarity.Legendary => "#f2a633",
			ItemRarity.Mythic => "#f24d8c",
			_ => "#ffffff"
		};
	}
}
