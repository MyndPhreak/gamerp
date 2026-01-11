using System.ComponentModel.DataAnnotations;

namespace GameRP.Api.DTOs;

/// <summary>
/// Request to deposit money into a wallet
/// </summary>
public class DepositRequestDto
{
    /// <summary>
    /// Amount to deposit (must be positive)
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Description of the deposit
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
}
