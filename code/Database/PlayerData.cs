using System;

/// <summary>
/// Player data model for persistence
/// </summary>
public class PlayerData
{
	public long SteamId { get; set; }
	public string Name { get; set; }
	public int Money { get; set; }
	public string JobTitle { get; set; }
	public DateTime LastSeen { get; set; }
}
