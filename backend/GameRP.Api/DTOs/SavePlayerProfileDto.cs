using System.ComponentModel.DataAnnotations;
namespace GameRP.Api.DTOs;

public class SavePlayerProfileDto
{
    [Required][MaxLength(100)] public string DisplayName { get; set; } = string.Empty;
    [MaxLength(20)] public string Gender { get; set; } = string.Empty;
    [MaxLength(20)] public string DateOfBirth { get; set; } = string.Empty;
    [Range(0f, 1f)] public float SkinTone { get; set; } = 0.5f;
    [Range(0f, 1f)] public float Height { get; set; } = 0.5f;
    [Range(0f, 1f)] public float Age { get; set; } = 0.5f;
    public int Money { get; set; }
    [MaxLength(100)] public string JobTitle { get; set; } = "Unemployed";
    public string ClothingList { get; set; } = "[]";
    public bool HasCompletedCharacterCreation { get; set; }
}
