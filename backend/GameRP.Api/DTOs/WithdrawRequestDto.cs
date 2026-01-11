using System.ComponentModel.DataAnnotations;

namespace GameRP.Api.DTOs;

/// <summary>
/// Request to withdraw money from a wallet
/// </summary>
public class WithdrawRequestDto
{
    /// <summary>
    /// Amount to withdraw (must be positive)
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Description of the withdrawal
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
}
