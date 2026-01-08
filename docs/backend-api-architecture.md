# ASP.NET Core Web API Backend Architecture

## Overview

This document outlines the architecture for an external ASP.NET Core Web API backend that serves as the economy system's persistence and business logic layer. The S&Box gamemode communicates with this API via HTTP/REST.

## Technology Stack

- **Framework**: ASP.NET Core 8.0 Web API
- **ORM**: Entity Framework Core 8.0
- **Database**: Microsoft SQL Server
- **Authentication**: JWT Bearer Tokens + Steam API
- **Architecture**: Clean Architecture with Event Sourcing
- **API Documentation**: Swagger/OpenAPI
- **Caching**: Redis (optional)
- **Logging**: Serilog

---

## Project Structure

```
GameRP.Api/
├── GameRP.Api/                          # Web API Project
│   ├── Controllers/                     # API Controllers
│   │   ├── WalletController.cs
│   │   ├── BankController.cs
│   │   ├── FederalReserveController.cs
│   │   ├── GovernmentController.cs
│   │   └── AdminController.cs
│   ├── Middleware/                      # Custom middleware
│   │   ├── AuthenticationMiddleware.cs
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   └── RateLimitingMiddleware.cs
│   ├── Filters/                         # Action filters
│   │   └── ValidateModelAttribute.cs
│   ├── Program.cs
│   └── appsettings.json
│
├── GameRP.Application/                  # Application Layer
│   ├── Commands/                        # CQRS Commands
│   │   ├── Wallet/
│   │   ├── Bank/
│   │   ├── FederalReserve/
│   │   └── Government/
│   ├── Queries/                         # CQRS Queries
│   │   ├── Wallet/
│   │   ├── Bank/
│   │   └── Economic/
│   ├── DTOs/                            # Data Transfer Objects
│   ├── Services/                        # Application services
│   ├── Validators/                      # FluentValidation validators
│   └── Interfaces/
│
├── GameRP.Domain/                       # Domain Layer
│   ├── Entities/                        # Domain entities
│   │   ├── Wallet.cs
│   │   ├── Bank.cs
│   │   ├── Loan.cs
│   │   ├── Transaction.cs
│   │   └── TaxRecord.cs
│   ├── Events/                          # Domain events
│   │   ├── WalletEvents.cs
│   │   ├── BankEvents.cs
│   │   └── TransactionEvents.cs
│   ├── ValueObjects/                    # Value objects
│   │   ├── Money.cs
│   │   └── SteamId.cs
│   ├── Aggregates/                      # Aggregate roots
│   └── Interfaces/
│
├── GameRP.Infrastructure/               # Infrastructure Layer
│   ├── Data/                            # EF Core
│   │   ├── GameRPDbContext.cs
│   │   ├── Configurations/              # Entity configurations
│   │   └── Migrations/
│   ├── Repositories/                    # Repository implementations
│   ├── EventStore/                      # Event sourcing implementation
│   └── ExternalServices/                # Steam API, etc.
│
└── GameRP.Tests/                        # Test projects
    ├── UnitTests/
    ├── IntegrationTests/
    └── E2ETests/
```

---

## Database Schema (EF Core Entities)

### Core Entities

```csharp
namespace GameRP.Domain.Entities
{
    /// <summary>
    /// Player wallet entity
    /// </summary>
    public class Wallet
    {
        public int Id { get; set; }
        public long SteamId { get; set; }
        public long Balance { get; set; }
        public long TotalEarned { get; set; }
        public long TotalSpent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Transaction> SentTransactions { get; set; }
        public virtual ICollection<Transaction> ReceivedTransactions { get; set; }
        public virtual ICollection<BankAccount> BankAccounts { get; set; }
    }

    /// <summary>
    /// Transaction record
    /// </summary>
    public class Transaction
    {
        public int Id { get; set; }
        public Guid TransactionId { get; set; } // Unique transaction identifier
        public long FromSteamId { get; set; }
        public long ToSteamId { get; set; }
        public long Amount { get; set; }
        public string TransactionType { get; set; } // Transfer, Tax, Salary, etc.
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }

        // Navigation properties
        public virtual Wallet FromWallet { get; set; }
        public virtual Wallet ToWallet { get; set; }
    }

    /// <summary>
    /// Bank entity
    /// </summary>
    public class Bank
    {
        public int Id { get; set; }
        public long OwnerSteamId { get; set; }
        public string BankName { get; set; }
        public long TotalDeposits { get; set; }
        public long TotalLoans { get; set; }
        public long Reserves { get; set; }
        public decimal DepositInterestRate { get; set; }
        public decimal LoanInterestRate { get; set; }
        public bool IsFailed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Calculated property
        public decimal ReserveRatio => TotalDeposits > 0 ? (decimal)Reserves / TotalDeposits : 1m;

        // Navigation properties
        public virtual ICollection<BankAccount> Accounts { get; set; }
        public virtual ICollection<Loan> Loans { get; set; }
    }

    /// <summary>
    /// Bank account entity
    /// </summary>
    public class BankAccount
    {
        public int Id { get; set; }
        public int BankId { get; set; }
        public long PlayerSteamId { get; set; }
        public long Balance { get; set; }
        public decimal InterestRate { get; set; }
        public string AccountType { get; set; } // Checking, Savings, CD
        public DateTime OpenedAt { get; set; }
        public DateTime LastInterestPaid { get; set; }

        // Navigation properties
        public virtual Bank Bank { get; set; }
        public virtual Wallet Wallet { get; set; }
    }

    /// <summary>
    /// Loan entity
    /// </summary>
    public class Loan
    {
        public int Id { get; set; }
        public Guid LoanId { get; set; }
        public int BankId { get; set; }
        public long BorrowerSteamId { get; set; }
        public long OriginalAmount { get; set; }
        public long RemainingBalance { get; set; }
        public decimal InterestRate { get; set; }
        public int TermWeeks { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime? NextPaymentDue { get; set; }
        public string Status { get; set; } // Active, PaidOff, Defaulted
        public string CollateralDescription { get; set; }

        // Navigation properties
        public virtual Bank Bank { get; set; }
        public virtual Wallet Borrower { get; set; }
        public virtual ICollection<LoanPayment> Payments { get; set; }
    }

    /// <summary>
    /// Loan payment record
    /// </summary>
    public class LoanPayment
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public long Amount { get; set; }
        public long PrincipalAmount { get; set; }
        public long InterestAmount { get; set; }
        public DateTime PaidAt { get; set; }

        // Navigation property
        public virtual Loan Loan { get; set; }
    }

    /// <summary>
    /// Federal Reserve singleton entity
    /// </summary>
    public class FederalReserve
    {
        public int Id { get; set; } // Always 1
        public long TotalGoldReserves { get; set; }
        public long TotalCurrencyInCirculation { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal BankLoanRate { get; set; }
        public decimal ReserveRequirement { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Gold deposit/withdrawal record
    /// </summary>
    public class GoldTransaction
    {
        public int Id { get; set; }
        public Guid TransactionId { get; set; }
        public long PlayerSteamId { get; set; }
        public int GoldAmount { get; set; }
        public long CurrencyAmount { get; set; }
        public decimal ExchangeRateUsed { get; set; }
        public string TransactionType { get; set; } // Deposit, Withdrawal
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Government entity
    /// </summary>
    public class Government
    {
        public int Id { get; set; } // Always 1
        public long TreasuryBalance { get; set; }
        public long TotalRevenueCollected { get; set; }
        public long TotalSpending { get; set; }
        public long OutstandingDebt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<TaxRecord> TaxRecords { get; set; }
        public virtual ICollection<GovernmentContract> Contracts { get; set; }
    }

    /// <summary>
    /// Tax collection record
    /// </summary>
    public class TaxRecord
    {
        public int Id { get; set; }
        public long PayerSteamId { get; set; }
        public long Amount { get; set; }
        public string TaxType { get; set; } // Income, Sales, Property, etc.
        public string Period { get; set; } // Week/Month
        public DateTime CollectedAt { get; set; }
        public bool IsPaid { get; set; }

        // Navigation property
        public virtual Government Government { get; set; }
    }

    /// <summary>
    /// Government contract
    /// </summary>
    public class GovernmentContract
    {
        public int Id { get; set; }
        public Guid ContractId { get; set; }
        public long ContractorSteamId { get; set; }
        public long Amount { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } // Pending, Active, Completed, Cancelled
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation property
        public virtual Government Government { get; set; }
    }

    /// <summary>
    /// Property entity
    /// </summary>
    public class Property
    {
        public int Id { get; set; }
        public long OwnerSteamId { get; set; }
        public string PropertyType { get; set; } // Residential, Commercial, etc.
        public long AssessedValue { get; set; }
        public long TaxOwed { get; set; }
        public DateTime LastTaxPayment { get; set; }
        public string LocationData { get; set; } // JSON with coordinates
        public DateTime PurchasedAt { get; set; }
    }

    /// <summary>
    /// Event store for event sourcing
    /// </summary>
    public class EventStore
    {
        public long Id { get; set; }
        public Guid EventId { get; set; }
        public string AggregateId { get; set; }
        public long Version { get; set; }
        public string EventType { get; set; }
        public string EventData { get; set; } // JSON
        public DateTime Timestamp { get; set; }

        // Indexes
        // Index on AggregateId, EventType, Timestamp
    }
}
```

---

## DbContext Configuration

```csharp
namespace GameRP.Infrastructure.Data
{
    using Microsoft.EntityFrameworkCore;
    using GameRP.Domain.Entities;

    public class GameRPDbContext : DbContext
    {
        public GameRPDbContext(DbContextOptions<GameRPDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Bank> Banks { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanPayment> LoanPayments { get; set; }
        public DbSet<FederalReserve> FederalReserve { get; set; }
        public DbSet<GoldTransaction> GoldTransactions { get; set; }
        public DbSet<Government> Government { get; set; }
        public DbSet<TaxRecord> TaxRecords { get; set; }
        public DbSet<GovernmentContract> GovernmentContracts { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<EventStore> EventStore { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations from separate files
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameRPDbContext).Assembly);

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Federal Reserve
            modelBuilder.Entity<FederalReserve>().HasData(new FederalReserve
            {
                Id = 1,
                TotalGoldReserves = 0,
                TotalCurrencyInCirculation = 0,
                ExchangeRate = 1000m,
                BankLoanRate = 0.05m,
                ReserveRequirement = 0.10m,
                UpdatedAt = DateTime.UtcNow
            });

            // Seed Government
            modelBuilder.Entity<Government>().HasData(new Government
            {
                Id = 1,
                TreasuryBalance = 0,
                TotalRevenueCollected = 0,
                TotalSpending = 0,
                OutstandingDebt = 0,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }
}
```

---

## Entity Configurations

```csharp
namespace GameRP.Infrastructure.Data.Configurations
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using GameRP.Domain.Entities;

    public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
    {
        public void Configure(EntityTypeBuilder<Wallet> builder)
        {
            builder.ToTable("Wallets");
            builder.HasKey(w => w.Id);

            builder.Property(w => w.SteamId)
                .IsRequired();

            builder.HasIndex(w => w.SteamId)
                .IsUnique();

            builder.Property(w => w.Balance)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(w => w.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(w => w.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            builder.HasMany(w => w.SentTransactions)
                .WithOne(t => t.FromWallet)
                .HasForeignKey(t => t.FromSteamId)
                .HasPrincipalKey(w => w.SteamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(w => w.ReceivedTransactions)
                .WithOne(t => t.ToWallet)
                .HasForeignKey(t => t.ToSteamId)
                .HasPrincipalKey(w => w.SteamId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("Transactions");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.TransactionId)
                .IsRequired();

            builder.HasIndex(t => t.TransactionId)
                .IsUnique();

            builder.HasIndex(t => t.Timestamp);
            builder.HasIndex(t => new { t.FromSteamId, t.Timestamp });
            builder.HasIndex(t => new { t.ToSteamId, t.Timestamp });

            builder.Property(t => t.TransactionType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.Description)
                .HasMaxLength(500);

            builder.Property(t => t.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }

    public class BankConfiguration : IEntityTypeConfiguration<Bank>
    {
        public void Configure(EntityTypeBuilder<Bank> builder)
        {
            builder.ToTable("Banks");
            builder.HasKey(b => b.Id);

            builder.Property(b => b.BankName)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(b => b.BankName)
                .IsUnique();

            builder.Property(b => b.DepositInterestRate)
                .HasPrecision(5, 4); // e.g., 0.0250 (2.5%)

            builder.Property(b => b.LoanInterestRate)
                .HasPrecision(5, 4);

            builder.Ignore(b => b.ReserveRatio); // Calculated property

            builder.HasMany(b => b.Accounts)
                .WithOne(a => a.Bank)
                .HasForeignKey(a => a.BankId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(b => b.Loans)
                .WithOne(l => l.Bank)
                .HasForeignKey(l => l.BankId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class EventStoreConfiguration : IEntityTypeConfiguration<EventStore>
    {
        public void Configure(EntityTypeBuilder<EventStore> builder)
        {
            builder.ToTable("EventStore");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.EventId)
                .IsRequired();

            builder.HasIndex(e => e.EventId)
                .IsUnique();

            builder.HasIndex(e => e.AggregateId);
            builder.HasIndex(e => e.EventType);
            builder.HasIndex(e => e.Timestamp);
            builder.HasIndex(e => new { e.AggregateId, e.Version })
                .IsUnique();

            builder.Property(e => e.EventData)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
```

---

## API Controllers

### Wallet Controller

```csharp
namespace GameRP.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using GameRP.Application.Commands.Wallet;
    using GameRP.Application.Queries.Wallet;
    using GameRP.Application.DTOs;

    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires JWT token
    public class WalletController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<WalletController> _logger;

        public WalletController(IMediator mediator, ILogger<WalletController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Get wallet balance for a player
        /// </summary>
        [HttpGet("{steamId}")]
        [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWallet(long steamId)
        {
            var query = new GetWalletQuery { SteamId = steamId };
            var result = await _mediator.Send(query);

            return result != null ? Ok(result) : NotFound();
        }

        /// <summary>
        /// Create a new wallet for a player
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(WalletDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateWallet([FromBody] CreateWalletCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetWallet), new { steamId = command.SteamId }, result.Data);
            }

            return BadRequest(result.ErrorMessage);
        }

        /// <summary>
        /// Transfer money between wallets
        /// </summary>
        [HttpPost("transfer")]
        [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TransferMoney([FromBody] TransferMoneyCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            return BadRequest(result.ErrorMessage);
        }

        /// <summary>
        /// Get transaction history for a wallet
        /// </summary>
        [HttpGet("{steamId}/transactions")]
        [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactions(
            long steamId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = new GetTransactionsQuery
            {
                SteamId = steamId,
                Page = page,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
```

### Federal Reserve Controller

```csharp
namespace GameRP.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using GameRP.Application.Commands.FederalReserve;
    using GameRP.Application.Queries.FederalReserve;
    using GameRP.Application.DTOs;

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FederalReserveController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FederalReserveController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get Federal Reserve economic data
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Public data
        [ProducesResponseType(typeof(FederalReserveDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFederalReserve()
        {
            var query = new GetFederalReserveQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Deposit gold to Federal Reserve
        /// </summary>
        [HttpPost("deposit-gold")]
        [ProducesResponseType(typeof(GoldTransactionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DepositGold([FromBody] DepositGoldCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            return BadRequest(result.ErrorMessage);
        }

        /// <summary>
        /// Withdraw gold from Federal Reserve
        /// </summary>
        [HttpPost("withdraw-gold")]
        [ProducesResponseType(typeof(GoldTransactionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> WithdrawGold([FromBody] WithdrawGoldCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            return BadRequest(result.ErrorMessage);
        }

        /// <summary>
        /// Update exchange rate (admin only)
        /// </summary>
        [HttpPut("exchange-rate")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateExchangeRate([FromBody] UpdateExchangeRateCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok();
            }

            return BadRequest(result.ErrorMessage);
        }
    }
}
```

### Bank Controller

```csharp
namespace GameRP.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using GameRP.Application.Commands.Bank;
    using GameRP.Application.Queries.Bank;
    using GameRP.Application.DTOs;

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BankController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BankController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Create a new bank
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(BankDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateBank([FromBody] CreateBankCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetBank), new { id = result.Data.Id }, result.Data);
            }

            return BadRequest(result.ErrorMessage);
        }

        /// <summary>
        /// Get bank by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BankDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBank(int id)
        {
            var query = new GetBankQuery { BankId = id };
            var result = await _mediator.Send(query);

            return result != null ? Ok(result) : NotFound();
        }

        /// <summary>
        /// Deposit money into bank account
        /// </summary>
        [HttpPost("{id}/deposit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Deposit(int id, [FromBody] DepositToBankCommand command)
        {
            command.BankId = id;
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            return BadRequest(result.ErrorMessage);
        }

        /// <summary>
        /// Withdraw money from bank account
        /// </summary>
        [HttpPost("{id}/withdraw")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Withdraw(int id, [FromBody] WithdrawFromBankCommand command)
        {
            command.BankId = id;
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            return BadRequest(result.ErrorMessage);
        }

        /// <summary>
        /// Request a loan from bank
        /// </summary>
        [HttpPost("{id}/loans")]
        [ProducesResponseType(typeof(LoanDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RequestLoan(int id, [FromBody] RequestLoanCommand command)
        {
            command.BankId = id;
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            return BadRequest(result.ErrorMessage);
        }

        /// <summary>
        /// Make a loan payment
        /// </summary>
        [HttpPost("loans/{loanId}/payment")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MakeLoanPayment(Guid loanId, [FromBody] MakeLoanPaymentCommand command)
        {
            command.LoanId = loanId;
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok();
            }

            return BadRequest(result.ErrorMessage);
        }
    }
}
```

---

## DTOs (Data Transfer Objects)

```csharp
namespace GameRP.Application.DTOs
{
    public class WalletDto
    {
        public long SteamId { get; set; }
        public long Balance { get; set; }
        public long TotalEarned { get; set; }
        public long TotalSpent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TransactionDto
    {
        public Guid TransactionId { get; set; }
        public long FromSteamId { get; set; }
        public long ToSteamId { get; set; }
        public long Amount { get; set; }
        public string TransactionType { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class BankDto
    {
        public int Id { get; set; }
        public string BankName { get; set; }
        public long OwnerSteamId { get; set; }
        public long TotalDeposits { get; set; }
        public long TotalLoans { get; set; }
        public long Reserves { get; set; }
        public decimal ReserveRatio { get; set; }
        public decimal DepositInterestRate { get; set; }
        public decimal LoanInterestRate { get; set; }
        public bool IsFailed { get; set; }
    }

    public class LoanDto
    {
        public Guid LoanId { get; set; }
        public int BankId { get; set; }
        public long BorrowerSteamId { get; set; }
        public long OriginalAmount { get; set; }
        public long RemainingBalance { get; set; }
        public decimal InterestRate { get; set; }
        public int TermWeeks { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime? NextPaymentDue { get; set; }
        public string Status { get; set; }
    }

    public class FederalReserveDto
    {
        public long TotalGoldReserves { get; set; }
        public long TotalCurrencyInCirculation { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal BankLoanRate { get; set; }
        public decimal ReserveRequirement { get; set; }
    }

    public class GoldTransactionDto
    {
        public Guid TransactionId { get; set; }
        public long PlayerSteamId { get; set; }
        public int GoldAmount { get; set; }
        public long CurrencyAmount { get; set; }
        public decimal ExchangeRateUsed { get; set; }
        public string TransactionType { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
```

---

## CQRS Commands (Examples)

```csharp
namespace GameRP.Application.Commands.Wallet
{
    using MediatR;

    public class CreateWalletCommand : IRequest<Result<WalletDto>>
    {
        public long SteamId { get; set; }
        public long InitialBalance { get; set; } = 0;
    }

    public class TransferMoneyCommand : IRequest<Result<TransactionDto>>
    {
        public long FromSteamId { get; set; }
        public long ToSteamId { get; set; }
        public long Amount { get; set; }
        public string Reason { get; set; }
    }

    // Command Handler
    public class TransferMoneyCommandHandler : IRequestHandler<TransferMoneyCommand, Result<TransactionDto>>
    {
        private readonly GameRPDbContext _context;
        private readonly IEventStore _eventStore;

        public TransferMoneyCommandHandler(GameRPDbContext context, IEventStore eventStore)
        {
            _context = context;
            _eventStore = eventStore;
        }

        public async Task<Result<TransactionDto>> Handle(TransferMoneyCommand request, CancellationToken cancellationToken)
        {
            // Start transaction
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Get both wallets
                var fromWallet = await _context.Wallets
                    .FirstOrDefaultAsync(w => w.SteamId == request.FromSteamId, cancellationToken);

                var toWallet = await _context.Wallets
                    .FirstOrDefaultAsync(w => w.SteamId == request.ToSteamId, cancellationToken);

                if (fromWallet == null || toWallet == null)
                    return Result<TransactionDto>.Failure("One or both wallets not found");

                // Validate balance
                if (fromWallet.Balance < request.Amount)
                    return Result<TransactionDto>.Failure("Insufficient funds");

                // Perform transfer
                fromWallet.Balance -= request.Amount;
                fromWallet.TotalSpent += request.Amount;
                fromWallet.UpdatedAt = DateTime.UtcNow;

                toWallet.Balance += request.Amount;
                toWallet.TotalEarned += request.Amount;
                toWallet.UpdatedAt = DateTime.UtcNow;

                // Create transaction record
                var txn = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    FromSteamId = request.FromSteamId,
                    ToSteamId = request.ToSteamId,
                    Amount = request.Amount,
                    TransactionType = "Transfer",
                    Description = request.Reason,
                    Timestamp = DateTime.UtcNow
                };

                _context.Transactions.Add(txn);

                // Save event to event store
                var @event = new MoneyTransferredEvent
                {
                    EventId = Guid.NewGuid(),
                    FromSteamId = request.FromSteamId,
                    ToSteamId = request.ToSteamId,
                    Amount = request.Amount,
                    Reason = request.Reason,
                    Timestamp = DateTime.UtcNow
                };

                await _eventStore.AppendAsync(@event, cancellationToken);

                // Commit
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // Return DTO
                var dto = new TransactionDto
                {
                    TransactionId = txn.TransactionId,
                    FromSteamId = txn.FromSteamId,
                    ToSteamId = txn.ToSteamId,
                    Amount = txn.Amount,
                    TransactionType = txn.TransactionType,
                    Description = txn.Description,
                    Timestamp = txn.Timestamp
                };

                return Result<TransactionDto>.Success(dto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<TransactionDto>.Failure($"Transfer failed: {ex.Message}");
            }
        }
    }
}
```

---

## Authentication (JWT + Steam)

```csharp
namespace GameRP.Api.Middleware
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using Microsoft.IdentityModel.Tokens;

    public class SteamAuthenticationService
    {
        private readonly IConfiguration _configuration;

        public SteamAuthenticationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Generate JWT token for authenticated S&Box server
        /// </summary>
        public string GenerateToken(long steamId, string serverKey)
        {
            // Verify server key (configured secret)
            var validServerKey = _configuration["Authentication:ServerKey"];
            if (serverKey != validServerKey)
            {
                throw new UnauthorizedAccessException("Invalid server key");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, steamId.ToString()),
                new Claim("server", "sbox"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Authentication:JwtSecret"]));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Authentication:Issuer"],
                audience: _configuration["Authentication:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
```

---

## Program.cs Configuration

```csharp
namespace GameRP.Api
{
    using Microsoft.EntityFrameworkCore;
    using GameRP.Infrastructure.Data;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.IdentityModel.Tokens;
    using System.Text;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Database
            builder.Services.AddDbContext<GameRPDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("GameRP.Infrastructure")
                )
            );

            // MediatR for CQRS
            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

            // JWT Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Authentication:Issuer"],
                        ValidAudience = builder.Configuration["Authentication:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Authentication:JwtSecret"])
                        )
                    };
                });

            // CORS for S&Box
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("SBoxPolicy", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Rate limiting
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<IRateLimitService, RateLimitService>();

            var app = builder.Build();

            // Configure pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("SBoxPolicy");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // Auto-migrate database
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<GameRPDbContext>();
                context.Database.Migrate();
            }

            app.Run();
        }
    }
}
```

---

## appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GameRPEconomy;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Authentication": {
    "JwtSecret": "your-super-secret-key-change-this-in-production",
    "ServerKey": "your-server-key-for-sbox",
    "Issuer": "GameRP.Api",
    "Audience": "GameRP.SBox"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## S&Box Integration Example

```csharp
// In S&Box gamemode
namespace GameRP.SBox
{
    using Sandbox;
    using System.Net.Http;
    using System.Text.Json;

    public class EconomyApiClient
    {
        private readonly string _baseUrl = "https://your-api-server.com/api";
        private readonly string _jwtToken;
        private readonly HttpClient _httpClient;

        public EconomyApiClient(string jwtToken)
        {
            _jwtToken = jwtToken;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwtToken}");
        }

        public async Task<long> GetBalance(long steamId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/wallet/{steamId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var wallet = JsonSerializer.Deserialize<WalletDto>(json);
                return wallet.Balance;
            }

            return 0;
        }

        public async Task<bool> TransferMoney(long fromSteamId, long toSteamId, long amount, string reason)
        {
            var command = new
            {
                FromSteamId = fromSteamId,
                ToSteamId = toSteamId,
                Amount = amount,
                Reason = reason
            };

            var json = JsonSerializer.Serialize(command);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/wallet/transfer", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SellGoldToFederalReserve(long steamId, int goldBars)
        {
            var command = new
            {
                PlayerSteamId = steamId,
                GoldBars = goldBars
            };

            var json = JsonSerializer.Serialize(command);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/federalreserve/deposit-gold", content);
            return response.IsSuccessStatusCode;
        }
    }

    // Usage in S&Box
    public class EconomyComponent : EntityComponent
    {
        private EconomyApiClient _apiClient;

        [Net] public long Balance { get; private set; }

        protected override void OnActivate()
        {
            if (Game.IsServer)
            {
                _apiClient = new EconomyApiClient(GetJwtToken());
                _ = UpdateBalance();
            }
        }

        private async Task UpdateBalance()
        {
            Balance = await _apiClient.GetBalance(Entity.SteamId);
        }

        public async Task<bool> PayPlayer(Player recipient, long amount)
        {
            var success = await _apiClient.TransferMoney(
                Entity.SteamId,
                recipient.SteamId,
                amount,
                "Player payment"
            );

            if (success)
            {
                await UpdateBalance();
            }

            return success;
        }
    }
}
```

---

## Next Steps

1. **Phase 1**: Set up basic project structure and DbContext
2. **Phase 2**: Implement Wallet endpoints and testing
3. **Phase 3**: Add Federal Reserve functionality
4. **Phase 4**: Implement Banking system
5. **Phase 5**: Add Government and taxation
6. **Phase 6**: Complete event sourcing integration
7. **Phase 7**: S&Box integration and testing
8. **Phase 8**: Deployment and production configuration
