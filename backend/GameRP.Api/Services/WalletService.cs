using GameRP.Api.Data;
using GameRP.Api.DTOs;
using GameRP.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameRP.Api.Services;

/// <summary>
/// Service for managing player wallets and transactions.
/// Implements event sourcing pattern where all balance changes are recorded as transactions.
/// </summary>
public class WalletService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WalletService> _logger;

    public WalletService(ApplicationDbContext context, ILogger<WalletService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get or create a wallet for a player by Steam ID
    /// </summary>
    public async Task<WalletDto?> GetOrCreateWalletAsync(long steamId, string? displayName = null)
    {
        _logger.LogInformation("GetOrCreateWallet for SteamID: {SteamId}", steamId);

        // Find or create player
        var player = await _context.Players
            .Include(p => p.Wallet)
            .FirstOrDefaultAsync(p => p.SteamId == steamId);

        if (player == null)
        {
            player = new Player
            {
                SteamId = steamId,
                DisplayName = displayName ?? $"Player_{steamId}",
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            };

            _context.Players.Add(player);
        }
        else
        {
            player.LastSeen = DateTime.UtcNow;
        }

        // Create wallet if doesn't exist
        if (player.Wallet == null)
        {
            player.Wallet = new Wallet
            {
                PlayerId = player.Id,
                SteamId = steamId,
                Balance = 1000, // Starting balance
                TotalEarned = 1000
            };

            _context.Wallets.Add(player.Wallet);

            // Record initial balance as a transaction
            var initialTransaction = new Transaction
            {
                PlayerId = player.Id,
                SteamId = steamId,
                Amount = 1000,
                BalanceAfter = 1000,
                Type = TransactionType.Reward,
                Description = "Starting balance"
            };

            _context.Transactions.Add(initialTransaction);
        }

        await _context.SaveChangesAsync();

        return MapToDto(player.Wallet);
    }

    /// <summary>
    /// Deposit money into a wallet
    /// </summary>
    public async Task<WalletDto?> DepositAsync(long steamId, DepositRequestDto request)
    {
        _logger.LogInformation("Deposit {Amount} for SteamID: {SteamId}", request.Amount, steamId);

        var player = await _context.Players
            .Include(p => p.Wallet)
            .FirstOrDefaultAsync(p => p.SteamId == steamId);

        if (player?.Wallet == null)
        {
            _logger.LogWarning("Wallet not found for SteamID: {SteamId}", steamId);
            return null;
        }

        // Update wallet
        player.Wallet.Balance += request.Amount;
        player.Wallet.TotalEarned += request.Amount;

        // Record transaction
        var transaction = new Transaction
        {
            PlayerId = player.Id,
            SteamId = steamId,
            Amount = request.Amount,
            BalanceAfter = player.Wallet.Balance,
            Type = TransactionType.Deposit,
            Description = request.Description ?? "Deposit"
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deposited {Amount}. New balance: {Balance}", request.Amount, player.Wallet.Balance);

        return MapToDto(player.Wallet);
    }

    /// <summary>
    /// Withdraw money from a wallet
    /// </summary>
    public async Task<WalletDto?> WithdrawAsync(long steamId, WithdrawRequestDto request)
    {
        _logger.LogInformation("Withdraw {Amount} for SteamID: {SteamId}", request.Amount, steamId);

        var player = await _context.Players
            .Include(p => p.Wallet)
            .FirstOrDefaultAsync(p => p.SteamId == steamId);

        if (player?.Wallet == null)
        {
            _logger.LogWarning("Wallet not found for SteamID: {SteamId}", steamId);
            return null;
        }

        // Check sufficient funds
        if (player.Wallet.Balance < request.Amount)
        {
            _logger.LogWarning("Insufficient funds. Balance: {Balance}, Requested: {Amount}",
                player.Wallet.Balance, request.Amount);
            throw new InvalidOperationException("Insufficient funds");
        }

        // Update wallet
        player.Wallet.Balance -= request.Amount;
        player.Wallet.TotalSpent += request.Amount;

        // Record transaction
        var transaction = new Transaction
        {
            PlayerId = player.Id,
            SteamId = steamId,
            Amount = -request.Amount, // Negative for withdrawal
            BalanceAfter = player.Wallet.Balance,
            Type = TransactionType.Withdrawal,
            Description = request.Description ?? "Withdrawal"
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Withdrew {Amount}. New balance: {Balance}", request.Amount, player.Wallet.Balance);

        return MapToDto(player.Wallet);
    }

    /// <summary>
    /// Transfer money from one wallet to another
    /// </summary>
    public async Task<WalletDto?> TransferAsync(long fromSteamId, TransferRequestDto request)
    {
        _logger.LogInformation("Transfer {Amount} from {FromSteamId} to {ToSteamId}",
            request.Amount, fromSteamId, request.ToSteamId);

        // Get both players
        var fromPlayer = await _context.Players
            .Include(p => p.Wallet)
            .FirstOrDefaultAsync(p => p.SteamId == fromSteamId);

        var toPlayer = await _context.Players
            .Include(p => p.Wallet)
            .FirstOrDefaultAsync(p => p.SteamId == request.ToSteamId);

        if (fromPlayer?.Wallet == null)
        {
            _logger.LogWarning("Sender wallet not found for SteamID: {SteamId}", fromSteamId);
            return null;
        }

        if (toPlayer?.Wallet == null)
        {
            _logger.LogWarning("Recipient wallet not found for SteamID: {SteamId}", request.ToSteamId);
            throw new InvalidOperationException("Recipient wallet not found");
        }

        // Check sufficient funds
        if (fromPlayer.Wallet.Balance < request.Amount)
        {
            _logger.LogWarning("Insufficient funds for transfer. Balance: {Balance}, Amount: {Amount}",
                fromPlayer.Wallet.Balance, request.Amount);
            throw new InvalidOperationException("Insufficient funds");
        }

        // Update sender wallet
        fromPlayer.Wallet.Balance -= request.Amount;
        fromPlayer.Wallet.TotalSpent += request.Amount;

        // Update recipient wallet
        toPlayer.Wallet.Balance += request.Amount;
        toPlayer.Wallet.TotalEarned += request.Amount;

        // Record transaction for sender
        var senderTransaction = new Transaction
        {
            PlayerId = fromPlayer.Id,
            SteamId = fromSteamId,
            Amount = -request.Amount,
            BalanceAfter = fromPlayer.Wallet.Balance,
            Type = TransactionType.Transfer,
            Description = request.Description ?? $"Transfer to {request.ToSteamId}",
            RelatedSteamId = request.ToSteamId
        };

        // Record transaction for recipient
        var recipientTransaction = new Transaction
        {
            PlayerId = toPlayer.Id,
            SteamId = request.ToSteamId,
            Amount = request.Amount,
            BalanceAfter = toPlayer.Wallet.Balance,
            Type = TransactionType.Transfer,
            Description = request.Description ?? $"Transfer from {fromSteamId}",
            RelatedSteamId = fromSteamId
        };

        _context.Transactions.AddRange(senderTransaction, recipientTransaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Transferred {Amount} from {FromSteamId} to {ToSteamId}",
            request.Amount, fromSteamId, request.ToSteamId);

        return MapToDto(fromPlayer.Wallet);
    }

    /// <summary>
    /// Map Wallet entity to DTO
    /// </summary>
    private static WalletDto MapToDto(Wallet wallet)
    {
        return new WalletDto
        {
            Id = wallet.Id,
            SteamId = wallet.SteamId,
            Balance = wallet.Balance,
            TotalEarned = wallet.TotalEarned,
            TotalSpent = wallet.TotalSpent,
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = wallet.UpdatedAt
        };
    }
}
