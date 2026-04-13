using System;

/// <summary>
/// Player data model for persistence
/// </summary>
public class PlayerData
{
	public long SteamId { get; set; }
	public string DisplayName { get; set; }
	public string Gender { get; set; }
	public string DateOfBirth { get; set; }
	public float SkinTone { get; set; } = 0.5f;
	public float Height { get; set; } = 0.5f;
	public float Age { get; set; } = 0.5f;
	public int Money { get; set; }
	public string JobTitle { get; set; }
	/// <summary>
	/// JSON array of equipped clothing resource paths
	/// </summary>
	public string ClothingList { get; set; } = "[]";
	public bool HasCompletedCharacterCreation { get; set; }
	public DateTime LastSeen { get; set; }
}
