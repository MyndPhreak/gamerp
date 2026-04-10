using GameRP.Api.Data;
using GameRP.Api.DTOs;
using GameRP.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameRP.Api.Services;

/// <summary>
/// Service for managing the Federal Reserve — gold deposits, withdrawals, and economic stats.
/// The Fed is a singleton entity that controls currency issuance backed by gold reserves.
/// </summary>
public class FederalReserveService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FederalReserveService> _logger;

    public FederalReserveService(ApplicationDbContext context, ILogger<FederalReserveService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get or create the singleton Federal Reserve entity
    /// </summary>
    public async Task<FederalReserve> GetOrCreateAsync()
    {
        var fed = await _context.FederalReserves.FirstOrDefaultAsync();
        if (fed != null)
            return fed;

        // Create singleton — use a transaction to handle races
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            fed = await _context.FederalReserves.FirstOrDefaultAsync();
            if (fed == null)
            {
                fed = new FederalReserve();
                _context.FederalReserves.Add(fed);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return fed;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning("Race condition creating Federal Reserve, retrying lookup");

            _context.ChangeTracker.Clear();
            return await _context.FederalReserves.FirstAsync();
        }
    }

    /// <summary>
    /// Get current Federal Reserve economic stats
    /// </summary>
    public async Task<FederalReserveStatsDto> GetStatsAsync()
    {
        var fed = await GetOrCreateAsync();
        return MapToStatsDto(fed);
    }

    /// <summary>
    /// Deposit gold bars at the Federal Reserve and receive currency.
    /// Creates currency backed by the deposited gold.
    /// </summary>
    public async Task<GoldOperationResultDto> DepositGoldAsync(long steamId, int goldBars)
    {
        if (goldBars <= 0)
            throw new ArgumentOutOfRangeException(nameof(goldBars), "Gold bars must be positive");

        _logger.LogInformation("Gold deposit: {GoldBars} bars from SteamID {SteamId}", goldBars, steamId);

        var fed = await GetOrCreateAsync();

        var player = await _context.Players
            .Include(p => p.Wallet)
            .FirstOrDefaultAsync(p => p.SteamId == steamId);

        if (player?.Wallet == null)
            throw new InvalidOperationException($"Wallet not found for SteamID: {steamId}");

        var currencyAmount = goldBars * fed.ExchangeRate;

        // Update Federal Reserve
        fed.TotalGoldReserves += goldBars;
        fed.TotalCurrencyInCirculation += currencyAmount;

        // Update player wallet
        player.Wallet.Balance += currencyAmount;
        player.Wallet.TotalEarned += currencyAmount;

        // Record transaction
        var txn = new Transaction
        {
            PlayerId = player.Id,
            SteamId = steamId,
            Amount = currencyAmount,
            BalanceAfter = player.Wallet.Balance,
            Type = TransactionType.GoldDeposit,
            Description = $"Deposited {goldBars} gold bar(s) at ${fed.ExchangeRate:N2}/bar"
        };

        _context.Transactions.Add(txn);
        CreateSnapshot(fed);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Gold deposit complete: {GoldBars} bars -> ${Currency:N2}. New balance: ${Balance:N2}",
            goldBars, currencyAmount, player.Wallet.Balance);

        return new GoldOperationResultDto
        {
            CurrencyAmount = currencyAmount,
            GoldBars = goldBars,
            NewWalletBalance = player.Wallet.Balance,
            FederalReserveStats = MapToStatsDto(fed)
        };
    }

    /// <summary>
    /// Withdraw gold bars from the Federal Reserve by paying currency.
    /// Destroys currency and releases gold from reserves.
    /// </summary>
    public async Task<GoldOperationResultDto> WithdrawGoldAsync(long steamId, int goldBars)
    {
        if (goldBars <= 0)
            throw new ArgumentOutOfRangeException(nameof(goldBars), "Gold bars must be positive");

        _logger.LogInformation("Gold withdrawal: {GoldBars} bars for SteamID {SteamId}", goldBars, steamId);

        var fed = await GetOrCreateAsync();

        if (fed.TotalGoldReserves < goldBars)
            throw new InvalidOperationException(
                $"Insufficient gold reserves. Available: {fed.TotalGoldReserves}, Requested: {goldBars}");

        var player = await _context.Players
            .Include(p => p.Wallet)
            .FirstOrDefaultAsync(p => p.SteamId == steamId);

        if (player?.Wallet == null)
            throw new InvalidOperationException($"Wallet not found for SteamID: {steamId}");

        var currencyCost = goldBars * fed.ExchangeRate;

        if (player.Wallet.Balance < currencyCost)
            throw new InvalidOperationException(
                $"Insufficient funds. Balance: ${player.Wallet.Balance:N2}, Cost: ${currencyCost:N2}");

        // Update Federal Reserve
        fed.TotalGoldReserves -= goldBars;
        fed.TotalCurrencyInCirculation -= currencyCost;

        // Update player wallet
        player.Wallet.Balance -= currencyCost;
        player.Wallet.TotalSpent += currencyCost;

        // Record transaction
        var txn = new Transaction
        {
            PlayerId = player.Id,
            SteamId = steamId,
            Amount = -currencyCost,
            BalanceAfter = player.Wallet.Balance,
            Type = TransactionType.GoldWithdrawal,
            Description = $"Withdrew {goldBars} gold bar(s) at ${fed.ExchangeRate:N2}/bar"
        };

        _context.Transactions.Add(txn);
        CreateSnapshot(fed);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Gold withdrawal complete: {GoldBars} bars for ${Currency:N2}. New balance: ${Balance:N2}",
            goldBars, currencyCost, player.Wallet.Balance);

        return new GoldOperationResultDto
        {
            CurrencyAmount = currencyCost,
            GoldBars = goldBars,
            NewWalletBalance = player.Wallet.Balance,
            FederalReserveStats = MapToStatsDto(fed)
        };
    }

    private static FederalReserveStatsDto MapToStatsDto(FederalReserve fed)
    {
        var goldValue = fed.TotalGoldReserves * fed.ExchangeRate;
        var backingRatio = fed.TotalCurrencyInCirculation > 0
            ? goldValue / fed.TotalCurrencyInCirculation
            : 1m;

        return new FederalReserveStatsDto
        {
            TotalGoldReserves = fed.TotalGoldReserves,
            TotalCurrencyInCirculation = fed.TotalCurrencyInCirculation,
            ExchangeRate = fed.ExchangeRate,
            GoldBackingRatio = backingRatio,
            UpdatedAt = fed.UpdatedAt
        };
    }

    private void CreateSnapshot(FederalReserve fed)
    {
        var goldValue = fed.TotalGoldReserves * fed.ExchangeRate;
        var backingRatio = fed.TotalCurrencyInCirculation > 0
            ? goldValue / fed.TotalCurrencyInCirculation
            : 1m;

        var snapshot = new FederalReserveSnapshot
        {
            TotalGoldReserves = fed.TotalGoldReserves,
            TotalCurrencyInCirculation = fed.TotalCurrencyInCirculation,
            ExchangeRate = fed.ExchangeRate,
            GoldBackingRatio = backingRatio
        };

        _context.FederalReserveSnapshots.Add(snapshot);
    }

    /// <summary>
    /// Get historical snapshots for rendering graphs
    /// </summary>
    public async Task<FederalReserveHistoryDto> GetHistoryAsync(int periods = 10)
    {
        var snapshots = await _context.FederalReserveSnapshots
            .OrderByDescending(s => s.CreatedAt)
            .Take(periods)
            .OrderBy(s => s.CreatedAt) // chronological for graphs
            .Select(s => new FederalReserveSnapshotDto
            {
                TotalGoldReserves = s.TotalGoldReserves,
                TotalCurrencyInCirculation = s.TotalCurrencyInCirculation,
                ExchangeRate = s.ExchangeRate,
                GoldBackingRatio = s.GoldBackingRatio,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        var fed = await GetOrCreateAsync();

        return new FederalReserveHistoryDto
        {
            Snapshots = snapshots,
            CurrentStats = MapToStatsDto(fed)
        };
    }

    /// <summary>
    /// Get recent gold transactions for the Fed dashboard
    /// </summary>
    public async Task<List<FederalReserveTransactionDto>> GetTransactionsAsync(int limit = 10, bool includePlayerInfo = false)
    {
        var query = _context.Transactions
            .Where(t => t.Type == TransactionType.GoldDeposit || t.Type == TransactionType.GoldWithdrawal)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit);

        if (includePlayerInfo)
        {
            query = query.Include(t => t.Player);
        }

        var transactions = await query.ToListAsync();

        return transactions.Select(t =>
        {
            // Parse gold bars from description (e.g., "Deposited 5 gold bar(s) at $1,000.00/bar")
            var goldBars = 0;
            if (t.Description != null)
            {
                var parts = t.Description.Split(' ');
                if (parts.Length > 1)
                    int.TryParse(parts[1], out goldBars);
            }

            return new FederalReserveTransactionDto
            {
                Type = t.Type == TransactionType.GoldDeposit ? "deposit" : "withdrawal",
                GoldBars = goldBars,
                CurrencyAmount = Math.Abs(t.Amount),
                Timestamp = t.CreatedAt,
                SteamId = includePlayerInfo ? t.SteamId : null,
                PlayerName = includePlayerInfo ? t.Player?.DisplayName : null
            };
        }).ToList();
    }

    /// <summary>
    /// Update the Federal Reserve exchange rate (admin only)
    /// </summary>
    public async Task<FederalReserveStatsDto> UpdateExchangeRateAsync(decimal newRate)
    {
        if (newRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(newRate), "Exchange rate must be positive");

        _logger.LogInformation("Exchange rate update: {NewRate}", newRate);

        var fed = await GetOrCreateAsync();
        var oldRate = fed.ExchangeRate;
        fed.ExchangeRate = newRate;

        CreateSnapshot(fed);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Exchange rate updated: ${OldRate:N2} -> ${NewRate:N2}", oldRate, newRate);

        return MapToStatsDto(fed);
    }
}
