using System.ComponentModel.DataAnnotations;

namespace GameRP.Api.Models;

/// <summary>
/// Base entity class with common properties for all database models
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier using GUID v7 (time-ordered)
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Timestamp when the entity was created (UTC)
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the entity was last updated (UTC)
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Row version for optimistic concurrency control.
    /// Automatically managed by EF Core using SQL Server's rowversion.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Soft delete flag - if true, entity is considered deleted
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Timestamp when the entity was soft-deleted (if applicable)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Generate a new GUID v7 (time-ordered GUID)
    /// </summary>
    protected static Guid NewGuidV7()
    {
        // GUID v7 implementation:
        // - First 48 bits: Unix timestamp in milliseconds
        // - Next 12 bits: Random data with version bits
        // - Remaining 62 bits: Random data with variant bits

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var guidBytes = new byte[16];

        // Fill with random data
        Random.Shared.NextBytes(guidBytes);

        // Set timestamp (first 6 bytes)
        var timestampBytes = BitConverter.GetBytes(timestamp);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timestampBytes);
        }

        Array.Copy(timestampBytes, 2, guidBytes, 0, 6);

        // Set version (4 bits) to 7
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x70);

        // Set variant (2 bits) to RFC 4122
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        return new Guid(guidBytes);
    }
}
