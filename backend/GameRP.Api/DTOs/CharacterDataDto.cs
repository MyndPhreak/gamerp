using System.ComponentModel.DataAnnotations;

namespace GameRP.Api.DTOs;

/// <summary>
/// DTO for saving character creation data
/// </summary>
public class CharacterDataDto
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Gender { get; set; } = "Male";

    [MaxLength(20)]
    public string DateOfBirth { get; set; } = string.Empty;

    [Range(0f, 1f)]
    public float SkinTone { get; set; } = 0.5f;

    [Range(0f, 1f)]
    public float Height { get; set; } = 0.5f;

    [Range(0f, 1f)]
    public float Age { get; set; } = 0.5f;

    public string ClothingList { get; set; } = "[]";
}
