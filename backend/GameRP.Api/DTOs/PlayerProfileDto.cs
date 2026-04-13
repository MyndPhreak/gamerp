namespace GameRP.Api.DTOs;

/// <summary>Full player profile DTO — returned by GET /profile and sent by PUT /profile</summary>
public class PlayerProfileDto
{
    public long SteamId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public float SkinTone { get; set; } = 0.5f;
    public float Height { get; set; } = 0.5f;
    public float Age { get; set; } = 0.5f;
    public int Money { get; set; }
    public string JobTitle { get; set; } = "Unemployed";
    public string ClothingList { get; set; } = "[]";
    public bool HasCompletedCharacterCreation { get; set; }
    public DateTime LastSeen { get; set; }
}
