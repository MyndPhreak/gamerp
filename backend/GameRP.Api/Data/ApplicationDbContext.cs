using GameRP.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameRP.Api.Data;

/// <summary>
/// Main database context for GameRP
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Player> Players { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Player configuration
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SteamId).IsUnique();
            entity.HasIndex(e => e.DisplayName);
            entity.HasIndex(e => e.LastSeen);
            entity.HasIndex(e => e.CreatedAt);

            // Soft delete query filter
            entity.HasQueryFilter(e => !e.IsDeleted);

            // One-to-one relationship with Wallet
            entity.HasOne(p => p.Wallet)
                .WithOne(w => w.Player)
                .HasForeignKey<Wallet>(w => w.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many relationship with Transactions
            entity.HasMany(p => p.Transactions)
                .WithOne(t => t.Player)
                .HasForeignKey(t => t.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Wallet configuration
        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PlayerId).IsUnique();
            entity.HasIndex(e => e.SteamId);
            entity.HasIndex(e => e.Balance);
            entity.HasIndex(e => e.CreatedAt);

            // Soft delete query filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PlayerId);
            entity.HasIndex(e => e.SteamId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Type);

            // Soft delete query filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }

            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
