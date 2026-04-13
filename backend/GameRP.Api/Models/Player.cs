using System.ComponentModel.DataAnnotations;

namespace GameRP.Api.Models;

/// <summary>
/// Represents a player in the game
/// </summary>
public class Player : BaseEntity
{
    [Required]
    public long SteamId { get; set; }

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;

    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    public int PlaytimeMinutes { get; set; } = 0;

    [MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    [MaxLength(20)]
    public string DateOfBirth { get; set; } = string.Empty;

    public float SkinTone { get; set; } = 0.5f;

    public float Height { get; set; } = 0.5f;

    public float Age { get; set; } = 0.5f;

    /// <summary>
    /// JSON array of equipped clothing resource paths
    /// </summary>
    public string ClothingList { get; set; } = "[]";

    public bool HasCompletedCharacterCreation { get; set; }

    [MaxLength(100)]
    public string JobTitle { get; set; } = "Unemployed";

    // Navigation properties
    public Wallet? Wallet { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public Player()
    {
        Id = NewGuidV7();
    }
}
