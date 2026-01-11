using System.ComponentModel.DataAnnotations;

namespace GameRP.Api.DTOs;

/// <summary>
/// Request to transfer money to another player
/// </summary>
public class TransferRequestDto
{
    /// <summary>
    /// Recipient's Steam ID
    /// </summary>
    [Required]
    public long ToSteamId { get; set; }

    /// <summary>
    /// Amount to transfer (must be positive)
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Description of the transfer
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
}
