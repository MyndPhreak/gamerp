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

    // Navigation properties
    public Wallet? Wallet { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public Player()
    {
        Id = NewGuidV7();
    }
}
