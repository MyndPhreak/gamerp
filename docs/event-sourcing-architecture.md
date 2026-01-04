# Event Sourcing Architecture for Economy System

## Overview

This document describes the event-sourced architecture for implementing the gold-backed economy system in S&Box using C#. Event sourcing stores all state changes as a sequence of events rather than storing current state, providing a complete audit trail and enabling powerful debugging capabilities.

## Core Concepts

### Event Sourcing Principles

**Traditional Approach:**
```
Player balance = $1,000 (current state stored)
```

**Event Sourcing Approach:**
```
Events:
1. PlayerCreated(id, initialBalance: 0)
2. MoneyDeposited(id, amount: 500)
3. MoneyDeposited(id, amount: 700)
4. MoneyWithdrawn(id, amount: 200)

Current balance = Replay all events = $1,000
```

**Benefits:**
- Complete audit trail
- Can rebuild state from events
- Time-travel debugging
- Natural transaction log
- Can create different projections from same events

---

## Architecture Layers

```
┌─────────────────────────────────────────┐
│         Commands (Intentions)           │
│  TransferMoney, DepositGold, PayTax     │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│      Command Handlers (Validation)      │
│   Validate business rules, create events│
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│      Events (Facts - Immutable)         │
│  MoneyTransferred, GoldDeposited        │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│         Event Store (Append-Only)       │
│    Persists all events permanently      │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│      Event Handlers (Side Effects)      │
│  Update read models, trigger actions    │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│      Read Models (Projections)          │
│   Optimized views for queries           │
└─────────────────────────────────────────┘
```

---

## Core Interfaces & Base Classes

### Event Base

```csharp
namespace GameRP.Economy.Core
{
    /// <summary>
    /// Base interface for all domain events
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Unique identifier for this event
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// When this event occurred (UTC)
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Aggregate ID this event belongs to
        /// </summary>
        string AggregateId { get; }

        /// <summary>
        /// Version/sequence number of this event for the aggregate
        /// </summary>
        long Version { get; }
    }

    /// <summary>
    /// Base implementation for all events
    /// </summary>
    public abstract record DomainEvent : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public required string AggregateId { get; init; }
        public required long Version { get; init; }
    }
}
```

### Aggregate Root

```csharp
namespace GameRP.Economy.Core
{
    /// <summary>
    /// Base class for all aggregate roots (entities that are event-sourced)
    /// </summary>
    public abstract class AggregateRoot
    {
        private readonly List<IEvent> _uncommittedEvents = new();

        public string Id { get; protected set; }
        public long Version { get; protected set; } = -1;

        /// <summary>
        /// Gets all events that haven't been persisted yet
        /// </summary>
        public IEnumerable<IEvent> GetUncommittedEvents() => _uncommittedEvents;

        /// <summary>
        /// Marks all uncommitted events as committed
        /// </summary>
        public void MarkEventsAsCommitted()
        {
            _uncommittedEvents.Clear();
        }

        /// <summary>
        /// Apply an event and add to uncommitted list
        /// </summary>
        protected void RaiseEvent(IEvent @event)
        {
            ApplyEvent(@event);
            _uncommittedEvents.Add(@event);
        }

        /// <summary>
        /// Apply event to update internal state (for replay)
        /// </summary>
        protected abstract void ApplyEvent(IEvent @event);

        /// <summary>
        /// Rebuild aggregate from event history
        /// </summary>
        public void LoadFromHistory(IEnumerable<IEvent> history)
        {
            foreach (var @event in history)
            {
                ApplyEvent(@event);
                Version = @event.Version;
            }
        }
    }
}
```

### Command Base

```csharp
namespace GameRP.Economy.Core
{
    /// <summary>
    /// Base interface for all commands (intentions to change state)
    /// </summary>
    public interface ICommand
    {
        Guid CommandId { get; }
        DateTime IssuedAt { get; }
    }

    /// <summary>
    /// Base implementation for commands
    /// </summary>
    public abstract record Command : ICommand
    {
        public Guid CommandId { get; init; } = Guid.NewGuid();
        public DateTime IssuedAt { get; init; } = DateTime.UtcNow;
    }
}
```

### Command Handler

```csharp
namespace GameRP.Economy.Core
{
    /// <summary>
    /// Handles a specific command type
    /// </summary>
    public interface ICommandHandler<TCommand> where TCommand : ICommand
    {
        Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of command execution
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; init; }
        public string ErrorMessage { get; init; }
        public List<IEvent> Events { get; init; } = new();

        public static Result Success(params IEvent[] events) => new()
        {
            IsSuccess = true,
            Events = events.ToList()
        };

        public static Result Failure(string error) => new()
        {
            IsSuccess = false,
            ErrorMessage = error
        };
    }
}
```

### Event Handler

```csharp
namespace GameRP.Economy.Core
{
    /// <summary>
    /// Handles a specific event type (side effects, projections)
    /// </summary>
    public interface IEventHandler<TEvent> where TEvent : IEvent
    {
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
    }
}
```

### Event Store

```csharp
namespace GameRP.Economy.Core
{
    /// <summary>
    /// Persists and retrieves events
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// Append events to the store
        /// </summary>
        Task AppendEventsAsync(string aggregateId, IEnumerable<IEvent> events, long expectedVersion);

        /// <summary>
        /// Get all events for an aggregate
        /// </summary>
        Task<IEnumerable<IEvent>> GetEventsAsync(string aggregateId);

        /// <summary>
        /// Get all events of a specific type
        /// </summary>
        Task<IEnumerable<TEvent>> GetEventsByTypeAsync<TEvent>() where TEvent : IEvent;

        /// <summary>
        /// Get all events after a specific version (for projections)
        /// </summary>
        Task<IEnumerable<IEvent>> GetEventsSinceVersionAsync(long version);
    }
}
```

### Repository

```csharp
namespace GameRP.Economy.Core
{
    /// <summary>
    /// Repository for loading and saving aggregates
    /// </summary>
    public interface IRepository<TAggregate> where TAggregate : AggregateRoot, new()
    {
        Task<TAggregate> GetByIdAsync(string id);
        Task SaveAsync(TAggregate aggregate);
    }

    /// <summary>
    /// Generic repository implementation
    /// </summary>
    public class EventSourcedRepository<TAggregate> : IRepository<TAggregate>
        where TAggregate : AggregateRoot, new()
    {
        private readonly IEventStore _eventStore;
        private readonly IEventBus _eventBus;

        public EventSourcedRepository(IEventStore eventStore, IEventBus eventBus)
        {
            _eventStore = eventStore;
            _eventBus = eventBus;
        }

        public async Task<TAggregate> GetByIdAsync(string id)
        {
            var events = await _eventStore.GetEventsAsync(id);
            var aggregate = new TAggregate();
            aggregate.LoadFromHistory(events);
            return aggregate;
        }

        public async Task SaveAsync(TAggregate aggregate)
        {
            var events = aggregate.GetUncommittedEvents();
            await _eventStore.AppendEventsAsync(aggregate.Id, events, aggregate.Version);

            // Publish events
            foreach (var @event in events)
            {
                await _eventBus.PublishAsync(@event);
            }

            aggregate.MarkEventsAsCommitted();
        }
    }
}
```

### Event Bus

```csharp
namespace GameRP.Economy.Core
{
    /// <summary>
    /// Publishes events to handlers
    /// </summary>
    public interface IEventBus
    {
        Task PublishAsync(IEvent @event);
        void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent;
    }

    /// <summary>
    /// Simple in-memory event bus implementation
    /// </summary>
    public class InMemoryEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<object>> _handlers = new();

        public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = new List<object>();
            }
            _handlers[eventType].Add(handler);
        }

        public async Task PublishAsync(IEvent @event)
        {
            var eventType = @event.GetType();
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    var handleMethod = handler.GetType().GetMethod("HandleAsync");
                    await (Task)handleMethod.Invoke(handler, new object[] { @event, CancellationToken.None });
                }
            }
        }
    }
}
```

---

## Wallet Aggregate

### Events

```csharp
namespace GameRP.Economy.Wallet.Events
{
    /// <summary>
    /// Player wallet was created
    /// </summary>
    public record WalletCreated : DomainEvent
    {
        public required long SteamId { get; init; }
        public required long InitialBalance { get; init; }
    }

    /// <summary>
    /// Money was added to wallet
    /// </summary>
    public record MoneyDeposited : DomainEvent
    {
        public required long Amount { get; init; }
        public required string Source { get; init; }
        public required string Reason { get; init; }
    }

    /// <summary>
    /// Money was removed from wallet
    /// </summary>
    public record MoneyWithdrawn : DomainEvent
    {
        public required long Amount { get; init; }
        public required string Destination { get; init; }
        public required string Reason { get; init; }
    }

    /// <summary>
    /// Money was transferred to another wallet
    /// </summary>
    public record MoneyTransferred : DomainEvent
    {
        public required long Amount { get; init; }
        public required string ToWalletId { get; init; }
        public required string Reason { get; init; }
    }

    /// <summary>
    /// Money was received from another wallet
    /// </summary>
    public record MoneyReceived : DomainEvent
    {
        public required long Amount { get; init; }
        public required string FromWalletId { get; init; }
        public required string Reason { get; init; }
    }
}
```

### Aggregate

```csharp
namespace GameRP.Economy.Wallet
{
    using GameRP.Economy.Wallet.Events;
    using GameRP.Economy.Core;

    /// <summary>
    /// Player wallet aggregate root
    /// </summary>
    public class WalletAggregate : AggregateRoot
    {
        public long SteamId { get; private set; }
        public long Balance { get; private set; }

        // For reconstitution
        public WalletAggregate() { }

        /// <summary>
        /// Create new wallet
        /// </summary>
        public WalletAggregate(long steamId, long initialBalance = 0)
        {
            Id = $"wallet-{steamId}";

            RaiseEvent(new WalletCreated
            {
                AggregateId = Id,
                Version = Version + 1,
                SteamId = steamId,
                InitialBalance = initialBalance
            });
        }

        /// <summary>
        /// Deposit money into wallet
        /// </summary>
        public Result Deposit(long amount, string source, string reason)
        {
            if (amount <= 0)
                return Result.Failure("Amount must be positive");

            RaiseEvent(new MoneyDeposited
            {
                AggregateId = Id,
                Version = Version + 1,
                Amount = amount,
                Source = source,
                Reason = reason
            });

            return Result.Success();
        }

        /// <summary>
        /// Withdraw money from wallet
        /// </summary>
        public Result Withdraw(long amount, string destination, string reason)
        {
            if (amount <= 0)
                return Result.Failure("Amount must be positive");

            if (Balance < amount)
                return Result.Failure($"Insufficient funds. Balance: {Balance}, Required: {amount}");

            RaiseEvent(new MoneyWithdrawn
            {
                AggregateId = Id,
                Version = Version + 1,
                Amount = amount,
                Destination = destination,
                Reason = reason
            });

            return Result.Success();
        }

        /// <summary>
        /// Transfer money to another wallet
        /// </summary>
        public Result Transfer(long amount, string toWalletId, string reason)
        {
            if (amount <= 0)
                return Result.Failure("Amount must be positive");

            if (Balance < amount)
                return Result.Failure("Insufficient funds");

            RaiseEvent(new MoneyTransferred
            {
                AggregateId = Id,
                Version = Version + 1,
                Amount = amount,
                ToWalletId = toWalletId,
                Reason = reason
            });

            return Result.Success();
        }

        /// <summary>
        /// Receive money from another wallet
        /// </summary>
        public void ReceiveMoney(long amount, string fromWalletId, string reason)
        {
            RaiseEvent(new MoneyReceived
            {
                AggregateId = Id,
                Version = Version + 1,
                Amount = amount,
                FromWalletId = fromWalletId,
                Reason = reason
            });
        }

        /// <summary>
        /// Apply events to rebuild state
        /// </summary>
        protected override void ApplyEvent(IEvent @event)
        {
            switch (@event)
            {
                case WalletCreated e:
                    Id = e.AggregateId;
                    SteamId = e.SteamId;
                    Balance = e.InitialBalance;
                    break;

                case MoneyDeposited e:
                    Balance += e.Amount;
                    break;

                case MoneyWithdrawn e:
                    Balance -= e.Amount;
                    break;

                case MoneyTransferred e:
                    Balance -= e.Amount;
                    break;

                case MoneyReceived e:
                    Balance += e.Amount;
                    break;
            }

            Version = @event.Version;
        }
    }
}
```

### Commands

```csharp
namespace GameRP.Economy.Wallet.Commands
{
    using GameRP.Economy.Core;

    public record CreateWallet : Command
    {
        public required long SteamId { get; init; }
        public long InitialBalance { get; init; } = 0;
    }

    public record DepositMoney : Command
    {
        public required string WalletId { get; init; }
        public required long Amount { get; init; }
        public required string Source { get; init; }
        public required string Reason { get; init; }
    }

    public record WithdrawMoney : Command
    {
        public required string WalletId { get; init; }
        public required long Amount { get; init; }
        public required string Destination { get; init; }
        public required string Reason { get; init; }
    }

    public record TransferMoney : Command
    {
        public required string FromWalletId { get; init; }
        public required string ToWalletId { get; init; }
        public required long Amount { get; init; }
        public required string Reason { get; init; }
    }
}
```

### Command Handlers

```csharp
namespace GameRP.Economy.Wallet.CommandHandlers
{
    using GameRP.Economy.Core;
    using GameRP.Economy.Wallet;
    using GameRP.Economy.Wallet.Commands;

    public class CreateWalletHandler : ICommandHandler<CreateWallet>
    {
        private readonly IRepository<WalletAggregate> _repository;

        public CreateWalletHandler(IRepository<WalletAggregate> repository)
        {
            _repository = repository;
        }

        public async Task<Result> HandleAsync(CreateWallet command, CancellationToken cancellationToken = default)
        {
            var wallet = new WalletAggregate(command.SteamId, command.InitialBalance);
            await _repository.SaveAsync(wallet);

            return Result.Success(wallet.GetUncommittedEvents().ToArray());
        }
    }

    public class TransferMoneyHandler : ICommandHandler<TransferMoney>
    {
        private readonly IRepository<WalletAggregate> _repository;
        private readonly IRateLimiter _rateLimiter;

        public TransferMoneyHandler(IRepository<WalletAggregate> repository, IRateLimiter rateLimiter)
        {
            _repository = repository;
            _rateLimiter = rateLimiter;
        }

        public async Task<Result> HandleAsync(TransferMoney command, CancellationToken cancellationToken = default)
        {
            // Rate limiting
            if (!_rateLimiter.AllowAction(command.FromWalletId, "transfer", 10))
                return Result.Failure("Rate limit exceeded");

            // Load both wallets
            var fromWallet = await _repository.GetByIdAsync(command.FromWalletId);
            var toWallet = await _repository.GetByIdAsync(command.ToWalletId);

            // Execute transfer
            var transferResult = fromWallet.Transfer(command.Amount, command.ToWalletId, command.Reason);
            if (!transferResult.IsSuccess)
                return transferResult;

            // Receive on other side
            toWallet.ReceiveMoney(command.Amount, command.FromWalletId, command.Reason);

            // Save both
            await _repository.SaveAsync(fromWallet);
            await _repository.SaveAsync(toWallet);

            return Result.Success();
        }
    }
}
```

---

## Federal Reserve Aggregate

### Events

```csharp
namespace GameRP.Economy.FederalReserve.Events
{
    using GameRP.Economy.Core;

    public record FederalReserveInitialized : DomainEvent
    {
        public required float InitialExchangeRate { get; init; }
    }

    public record GoldDeposited : DomainEvent
    {
        public required long PlayerSteamId { get; init; }
        public required int GoldBars { get; init; }
        public required long CurrencyIssued { get; init; }
        public required float ExchangeRateUsed { get; init; }
    }

    public record GoldWithdrawn : DomainEvent
    {
        public required long PlayerSteamId { get; init; }
        public required int GoldBars { get; init; }
        public required long CurrencyBurned { get; init; }
        public required float ExchangeRateUsed { get; init; }
    }

    public record ExchangeRateChanged : DomainEvent
    {
        public required float OldRate { get; init; }
        public required float NewRate { get; init; }
        public required string Reason { get; init; }
    }
}
```

### Aggregate

```csharp
namespace GameRP.Economy.FederalReserve
{
    using GameRP.Economy.Core;
    using GameRP.Economy.FederalReserve.Events;

    public class FederalReserveAggregate : AggregateRoot
    {
        public long TotalGoldReserves { get; private set; }
        public long TotalCurrencyInCirculation { get; private set; }
        public float ExchangeRate { get; private set; }

        public FederalReserveAggregate() { }

        public FederalReserveAggregate(string id, float initialExchangeRate = 1000f)
        {
            Id = id;

            RaiseEvent(new FederalReserveInitialized
            {
                AggregateId = Id,
                Version = 0,
                InitialExchangeRate = initialExchangeRate
            });
        }

        public Result DepositGold(long playerSteamId, int goldBars)
        {
            if (goldBars <= 0)
                return Result.Failure("Gold amount must be positive");

            long currencyToIssue = (long)(goldBars * ExchangeRate);

            RaiseEvent(new GoldDeposited
            {
                AggregateId = Id,
                Version = Version + 1,
                PlayerSteamId = playerSteamId,
                GoldBars = goldBars,
                CurrencyIssued = currencyToIssue,
                ExchangeRateUsed = ExchangeRate
            });

            return Result.Success();
        }

        public Result WithdrawGold(long playerSteamId, int goldBars)
        {
            if (goldBars <= 0)
                return Result.Failure("Gold amount must be positive");

            if (TotalGoldReserves < goldBars)
                return Result.Failure("Insufficient gold reserves");

            long currencyToBurn = (long)(goldBars * ExchangeRate);

            RaiseEvent(new GoldWithdrawn
            {
                AggregateId = Id,
                Version = Version + 1,
                PlayerSteamId = playerSteamId,
                GoldBars = goldBars,
                CurrencyBurned = currencyToBurn,
                ExchangeRateUsed = ExchangeRate
            });

            return Result.Success();
        }

        public Result ChangeExchangeRate(float newRate, string reason)
        {
            if (newRate <= 0)
                return Result.Failure("Exchange rate must be positive");

            RaiseEvent(new ExchangeRateChanged
            {
                AggregateId = Id,
                Version = Version + 1,
                OldRate = ExchangeRate,
                NewRate = newRate,
                Reason = reason
            });

            return Result.Success();
        }

        protected override void ApplyEvent(IEvent @event)
        {
            switch (@event)
            {
                case FederalReserveInitialized e:
                    ExchangeRate = e.InitialExchangeRate;
                    break;

                case GoldDeposited e:
                    TotalGoldReserves += e.GoldBars;
                    TotalCurrencyInCirculation += e.CurrencyIssued;
                    break;

                case GoldWithdrawn e:
                    TotalGoldReserves -= e.GoldBars;
                    TotalCurrencyInCirculation -= e.CurrencyBurned;
                    break;

                case ExchangeRateChanged e:
                    ExchangeRate = e.NewRate;
                    break;
            }

            Version = @event.Version;
        }
    }
}
```

---

## Bank Aggregate

### Events

```csharp
namespace GameRP.Economy.Banking.Events
{
    using GameRP.Economy.Core;

    public record BankCreated : DomainEvent
    {
        public required long OwnerSteamId { get; init; }
        public required string BankName { get; init; }
        public required long InitialCapital { get; init; }
    }

    public record DepositAccepted : DomainEvent
    {
        public required long PlayerSteamId { get; init; }
        public required long Amount { get; init; }
        public required string AccountId { get; init; }
    }

    public record WithdrawalProcessed : DomainEvent
    {
        public required long PlayerSteamId { get; init; }
        public required long Amount { get; init; }
        public required string AccountId { get; init; }
    }

    public record LoanIssued : DomainEvent
    {
        public required string LoanId { get; init; }
        public required long BorrowerSteamId { get; init; }
        public required long Amount { get; init; }
        public required float InterestRate { get; init; }
        public required int TermWeeks { get; init; }
    }

    public record LoanPaymentReceived : DomainEvent
    {
        public required string LoanId { get; init; }
        public required long Amount { get; init; }
        public required long Principal { get; init; }
        public required long Interest { get; init; }
    }

    public record LoanDefaulted : DomainEvent
    {
        public required string LoanId { get; init; }
        public required long RemainingBalance { get; init; }
    }

    public record InterestPaid : DomainEvent
    {
        public required string AccountId { get; init; }
        public required long Amount { get; init; }
    }

    public record BankFailed : DomainEvent
    {
        public required string Reason { get; init; }
        public required long TotalDeposits { get; init; }
        public required long AvailableReserves { get; init; }
    }
}
```

### Aggregate

```csharp
namespace GameRP.Economy.Banking
{
    using GameRP.Economy.Core;
    using GameRP.Economy.Banking.Events;

    public class BankAggregate : AggregateRoot
    {
        public long OwnerSteamId { get; private set; }
        public string BankName { get; private set; }
        public long TotalDeposits { get; private set; }
        public long TotalLoans { get; private set; }
        public long Reserves { get; private set; }
        public float DepositInterestRate { get; private set; } = 0.02f;
        public float LoanInterestRate { get; private set; } = 0.08f;
        public bool IsFailed { get; private set; }

        private const float RESERVE_REQUIREMENT = 0.10f;

        public float ReserveRatio => TotalDeposits > 0 ? (float)Reserves / TotalDeposits : 1f;

        public BankAggregate() { }

        public BankAggregate(string id, long ownerSteamId, string bankName, long initialCapital)
        {
            Id = id;

            RaiseEvent(new BankCreated
            {
                AggregateId = Id,
                Version = 0,
                OwnerSteamId = ownerSteamId,
                BankName = bankName,
                InitialCapital = initialCapital
            });
        }

        public Result AcceptDeposit(long playerSteamId, long amount, string accountId)
        {
            if (IsFailed)
                return Result.Failure("Bank has failed");

            if (amount <= 0)
                return Result.Failure("Amount must be positive");

            RaiseEvent(new DepositAccepted
            {
                AggregateId = Id,
                Version = Version + 1,
                PlayerSteamId = playerSteamId,
                Amount = amount,
                AccountId = accountId
            });

            return Result.Success();
        }

        public Result ProcessWithdrawal(long playerSteamId, long amount, string accountId)
        {
            if (IsFailed)
                return Result.Failure("Bank has failed");

            if (amount <= 0)
                return Result.Failure("Amount must be positive");

            if (Reserves < amount)
            {
                // Bank run!
                RaiseEvent(new BankFailed
                {
                    AggregateId = Id,
                    Version = Version + 1,
                    Reason = "Insufficient reserves for withdrawal",
                    TotalDeposits = TotalDeposits,
                    AvailableReserves = Reserves
                });
                return Result.Failure("Bank has insufficient reserves");
            }

            RaiseEvent(new WithdrawalProcessed
            {
                AggregateId = Id,
                Version = Version + 1,
                PlayerSteamId = playerSteamId,
                Amount = amount,
                AccountId = accountId
            });

            return Result.Success();
        }

        public Result IssueLoan(string loanId, long borrowerSteamId, long amount, int termWeeks)
        {
            if (IsFailed)
                return Result.Failure("Bank has failed");

            if (amount <= 0)
                return Result.Failure("Amount must be positive");

            // Check reserve requirement
            long reservesAfterLoan = Reserves - amount;
            float projectedRatio = TotalDeposits > 0 ? (float)reservesAfterLoan / TotalDeposits : 0f;

            if (projectedRatio < RESERVE_REQUIREMENT)
                return Result.Failure($"Loan would violate reserve requirement. Current ratio: {ReserveRatio:P}, Required: {RESERVE_REQUIREMENT:P}");

            RaiseEvent(new LoanIssued
            {
                AggregateId = Id,
                Version = Version + 1,
                LoanId = loanId,
                BorrowerSteamId = borrowerSteamId,
                Amount = amount,
                InterestRate = LoanInterestRate,
                TermWeeks = termWeeks
            });

            return Result.Success();
        }

        public Result ReceiveLoanPayment(string loanId, long amount, long principal, long interest)
        {
            if (amount <= 0)
                return Result.Failure("Amount must be positive");

            RaiseEvent(new LoanPaymentReceived
            {
                AggregateId = Id,
                Version = Version + 1,
                LoanId = loanId,
                Amount = amount,
                Principal = principal,
                Interest = interest
            });

            return Result.Success();
        }

        protected override void ApplyEvent(IEvent @event)
        {
            switch (@event)
            {
                case BankCreated e:
                    OwnerSteamId = e.OwnerSteamId;
                    BankName = e.BankName;
                    Reserves = e.InitialCapital;
                    break;

                case DepositAccepted e:
                    TotalDeposits += e.Amount;
                    Reserves += e.Amount;
                    break;

                case WithdrawalProcessed e:
                    TotalDeposits -= e.Amount;
                    Reserves -= e.Amount;
                    break;

                case LoanIssued e:
                    TotalLoans += e.Amount;
                    Reserves -= e.Amount;
                    break;

                case LoanPaymentReceived e:
                    Reserves += e.Amount;
                    TotalLoans -= e.Principal;
                    // Interest is profit for bank
                    break;

                case BankFailed e:
                    IsFailed = true;
                    break;
            }

            Version = @event.Version;
        }
    }
}
```

---

## Government Aggregate

### Events

```csharp
namespace GameRP.Economy.Government.Events
{
    using GameRP.Economy.Core;

    public record GovernmentInitialized : DomainEvent
    {
        public required long InitialTreasury { get; init; }
    }

    public record TaxCollected : DomainEvent
    {
        public required long Amount { get; init; }
        public required string TaxType { get; init; }
        public required long PayerSteamId { get; init; }
    }

    public record SalaryPaid : DomainEvent
    {
        public required long EmployeeSteamId { get; init; }
        public required long GrossAmount { get; init; }
        public required long TaxWithheld { get; init; }
        public required long NetAmount { get; init; }
        public required string JobTitle { get; init; }
    }

    public record ContractAwarded : DomainEvent
    {
        public required string ContractId { get; init; }
        public required long ContractorSteamId { get; init; }
        public required long Amount { get; init; }
        public required string Description { get; init; }
    }

    public record BudgetAllocated : DomainEvent
    {
        public required string Category { get; init; }
        public required long Amount { get; init; }
        public required string Period { get; init; }
    }

    public record DebtIncurred : DomainEvent
    {
        public required long Amount { get; init; }
        public required float InterestRate { get; init; }
        public required int TermWeeks { get; init; }
        public required string Reason { get; init; }
    }
}
```

### Aggregate

```csharp
namespace GameRP.Economy.Government
{
    using GameRP.Economy.Core;
    using GameRP.Economy.Government.Events;

    public class GovernmentAggregate : AggregateRoot
    {
        public long TreasuryBalance { get; private set; }
        public long TotalRevenueCollected { get; private set; }
        public long TotalSpending { get; private set; }
        public long OutstandingDebt { get; private set; }

        public GovernmentAggregate() { }

        public GovernmentAggregate(string id, long initialTreasury = 0)
        {
            Id = id;

            RaiseEvent(new GovernmentInitialized
            {
                AggregateId = Id,
                Version = 0,
                InitialTreasury = initialTreasury
            });
        }

        public Result CollectTax(long amount, string taxType, long payerSteamId)
        {
            if (amount <= 0)
                return Result.Failure("Tax amount must be positive");

            RaiseEvent(new TaxCollected
            {
                AggregateId = Id,
                Version = Version + 1,
                Amount = amount,
                TaxType = taxType,
                PayerSteamId = payerSteamId
            });

            return Result.Success();
        }

        public Result PaySalary(long employeeSteamId, long grossAmount, long taxWithheld, string jobTitle)
        {
            long netAmount = grossAmount - taxWithheld;

            if (TreasuryBalance < netAmount)
                return Result.Failure("Insufficient treasury funds");

            RaiseEvent(new SalaryPaid
            {
                AggregateId = Id,
                Version = Version + 1,
                EmployeeSteamId = employeeSteamId,
                GrossAmount = grossAmount,
                TaxWithheld = taxWithheld,
                NetAmount = netAmount,
                JobTitle = jobTitle
            });

            return Result.Success();
        }

        public Result AwardContract(string contractId, long contractorSteamId, long amount, string description)
        {
            if (TreasuryBalance < amount)
                return Result.Failure("Insufficient treasury funds");

            RaiseEvent(new ContractAwarded
            {
                AggregateId = Id,
                Version = Version + 1,
                ContractId = contractId,
                ContractorSteamId = contractorSteamId,
                Amount = amount,
                Description = description
            });

            return Result.Success();
        }

        protected override void ApplyEvent(IEvent @event)
        {
            switch (@event)
            {
                case GovernmentInitialized e:
                    TreasuryBalance = e.InitialTreasury;
                    break;

                case TaxCollected e:
                    TreasuryBalance += e.Amount;
                    TotalRevenueCollected += e.Amount;
                    break;

                case SalaryPaid e:
                    TreasuryBalance -= e.NetAmount;
                    TotalSpending += e.NetAmount;
                    // Tax withheld stays in treasury
                    break;

                case ContractAwarded e:
                    TreasuryBalance -= e.Amount;
                    TotalSpending += e.Amount;
                    break;
            }

            Version = @event.Version;
        }
    }
}
```

---

## Projections (Read Models)

### Wallet Balance Projection

```csharp
namespace GameRP.Economy.Projections
{
    using GameRP.Economy.Core;
    using GameRP.Economy.Wallet.Events;

    /// <summary>
    /// Denormalized view of wallet balances for quick queries
    /// </summary>
    public class WalletBalanceProjection : IEventHandler<WalletCreated>,
                                            IEventHandler<MoneyDeposited>,
                                            IEventHandler<MoneyWithdrawn>,
                                            IEventHandler<MoneyTransferred>,
                                            IEventHandler<MoneyReceived>
    {
        private readonly Dictionary<string, WalletBalanceView> _balances = new();

        public Task HandleAsync(WalletCreated @event, CancellationToken cancellationToken = default)
        {
            _balances[@event.AggregateId] = new WalletBalanceView
            {
                WalletId = @event.AggregateId,
                SteamId = @event.SteamId,
                Balance = @event.InitialBalance,
                LastUpdated = @event.Timestamp
            };
            return Task.CompletedTask;
        }

        public Task HandleAsync(MoneyDeposited @event, CancellationToken cancellationToken = default)
        {
            if (_balances.TryGetValue(@event.AggregateId, out var view))
            {
                view.Balance += @event.Amount;
                view.LastUpdated = @event.Timestamp;
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(MoneyWithdrawn @event, CancellationToken cancellationToken = default)
        {
            if (_balances.TryGetValue(@event.AggregateId, out var view))
            {
                view.Balance -= @event.Amount;
                view.LastUpdated = @event.Timestamp;
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(MoneyTransferred @event, CancellationToken cancellationToken = default)
        {
            if (_balances.TryGetValue(@event.AggregateId, out var view))
            {
                view.Balance -= @event.Amount;
                view.LastUpdated = @event.Timestamp;
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(MoneyReceived @event, CancellationToken cancellationToken = default)
        {
            if (_balances.TryGetValue(@event.AggregateId, out var view))
            {
                view.Balance += @event.Amount;
                view.LastUpdated = @event.Timestamp;
            }
            return Task.CompletedTask;
        }

        public WalletBalanceView GetBalance(string walletId)
        {
            return _balances.TryGetValue(walletId, out var view) ? view : null;
        }

        public WalletBalanceView GetBalanceBySteamId(long steamId)
        {
            return _balances.Values.FirstOrDefault(v => v.SteamId == steamId);
        }
    }

    public class WalletBalanceView
    {
        public string WalletId { get; set; }
        public long SteamId { get; set; }
        public long Balance { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
```

### Economic Stats Projection

```csharp
namespace GameRP.Economy.Projections
{
    using GameRP.Economy.Core;
    using GameRP.Economy.FederalReserve.Events;
    using GameRP.Economy.Government.Events;

    /// <summary>
    /// Aggregated economic statistics
    /// </summary>
    public class EconomicStatsProjection : IEventHandler<GoldDeposited>,
                                            IEventHandler<GoldWithdrawn>,
                                            IEventHandler<TaxCollected>
    {
        public long TotalCurrencyInCirculation { get; private set; }
        public long TotalGoldReserves { get; private set; }
        public long TotalTaxRevenue { get; private set; }
        public long MoneyCreatedToday { get; private set; }
        public long MoneyDestroyedToday { get; private set; }

        private DateTime _lastDayReset = DateTime.UtcNow.Date;

        public Task HandleAsync(GoldDeposited @event, CancellationToken cancellationToken = default)
        {
            ResetDailyCountersIfNeeded();
            TotalCurrencyInCirculation += @event.CurrencyIssued;
            TotalGoldReserves += @event.GoldBars;
            MoneyCreatedToday += @event.CurrencyIssued;
            return Task.CompletedTask;
        }

        public Task HandleAsync(GoldWithdrawn @event, CancellationToken cancellationToken = default)
        {
            ResetDailyCountersIfNeeded();
            TotalCurrencyInCirculation -= @event.CurrencyBurned;
            TotalGoldReserves -= @event.GoldBars;
            MoneyDestroyedToday += @event.CurrencyBurned;
            return Task.CompletedTask;
        }

        public Task HandleAsync(TaxCollected @event, CancellationToken cancellationToken = default)
        {
            TotalTaxRevenue += @event.Amount;
            return Task.CompletedTask;
        }

        private void ResetDailyCountersIfNeeded()
        {
            var today = DateTime.UtcNow.Date;
            if (today > _lastDayReset)
            {
                MoneyCreatedToday = 0;
                MoneyDestroyedToday = 0;
                _lastDayReset = today;
            }
        }
    }
}
```

---

## Process Managers (Sagas)

### Transfer Money Process

```csharp
namespace GameRP.Economy.ProcessManagers
{
    using GameRP.Economy.Core;
    using GameRP.Economy.Wallet.Events;
    using GameRP.Economy.Wallet.Commands;

    /// <summary>
    /// Coordinates money transfer between two wallets
    /// </summary>
    public class TransferMoneyProcess : IEventHandler<MoneyTransferred>
    {
        private readonly ICommandHandler<DepositMoney> _depositHandler;

        public TransferMoneyProcess(ICommandHandler<DepositMoney> depositHandler)
        {
            _depositHandler = depositHandler;
        }

        public async Task HandleAsync(MoneyTransferred @event, CancellationToken cancellationToken = default)
        {
            // When money is transferred out, deposit it to the recipient
            var depositCommand = new DepositMoney
            {
                WalletId = @event.ToWalletId,
                Amount = @event.Amount,
                Source = @event.AggregateId,
                Reason = @event.Reason
            };

            await _depositHandler.HandleAsync(depositCommand, cancellationToken);
        }
    }
}
```

### Gold Deposit Process

```csharp
namespace GameRP.Economy.ProcessManagers
{
    using GameRP.Economy.Core;
    using GameRP.Economy.FederalReserve.Events;
    using GameRP.Economy.Wallet.Commands;

    /// <summary>
    /// When gold is deposited, issue currency to player
    /// </summary>
    public class GoldDepositProcess : IEventHandler<GoldDeposited>
    {
        private readonly ICommandHandler<DepositMoney> _depositHandler;

        public GoldDepositProcess(ICommandHandler<DepositMoney> depositHandler)
        {
            _depositHandler = depositHandler;
        }

        public async Task HandleAsync(GoldDeposited @event, CancellationToken cancellationToken = default)
        {
            var walletId = $"wallet-{@event.PlayerSteamId}";

            var depositCommand = new DepositMoney
            {
                WalletId = walletId,
                Amount = @event.CurrencyIssued,
                Source = "FederalReserve",
                Reason = $"Gold deposit: {@event.GoldBars} bars"
            };

            await _depositHandler.HandleAsync(depositCommand, cancellationToken);
        }
    }
}
```

---

## Service Layer

### Wallet Service

```csharp
namespace GameRP.Economy.Services
{
    using GameRP.Economy.Core;
    using GameRP.Economy.Wallet.Commands;
    using GameRP.Economy.Projections;

    /// <summary>
    /// High-level wallet operations
    /// </summary>
    public class WalletService
    {
        private readonly ICommandHandler<CreateWallet> _createHandler;
        private readonly ICommandHandler<TransferMoney> _transferHandler;
        private readonly WalletBalanceProjection _balanceProjection;

        public WalletService(
            ICommandHandler<CreateWallet> createHandler,
            ICommandHandler<TransferMoney> transferHandler,
            WalletBalanceProjection balanceProjection)
        {
            _createHandler = createHandler;
            _transferHandler = transferHandler;
            _balanceProjection = balanceProjection;
        }

        public async Task<Result> CreateWalletForPlayer(long steamId)
        {
            var command = new CreateWallet { SteamId = steamId };
            return await _createHandler.HandleAsync(command);
        }

        public async Task<Result> TransferMoney(long fromSteamId, long toSteamId, long amount, string reason)
        {
            var fromWalletId = $"wallet-{fromSteamId}";
            var toWalletId = $"wallet-{toSteamId}";

            var command = new TransferMoney
            {
                FromWalletId = fromWalletId,
                ToWalletId = toWalletId,
                Amount = amount,
                Reason = reason
            };

            return await _transferHandler.HandleAsync(command);
        }

        public long GetBalance(long steamId)
        {
            var view = _balanceProjection.GetBalanceBySteamId(steamId);
            return view?.Balance ?? 0;
        }
    }
}
```

---

## S&Box Integration

### Player Component

```csharp
using Sandbox;
using GameRP.Economy.Services;

namespace GameRP.Economy.SandboxIntegration
{
    /// <summary>
    /// Player component that interfaces with event-sourced wallet
    /// </summary>
    public class EconomyPlayerComponent : EntityComponent<Player>
    {
        private WalletService _walletService;

        [Net] public long Balance { get; private set; }

        protected override void OnActivate()
        {
            base.OnActivate();

            if (Game.IsServer)
            {
                _walletService = ServiceLocator.Get<WalletService>();
                InitializeWallet();
            }
        }

        private async void InitializeWallet()
        {
            var result = await _walletService.CreateWalletForPlayer(Entity.SteamId);
            if (result.IsSuccess)
            {
                Log.Info($"Wallet created for player {Entity.SteamId}");
            }
        }

        [GameEvent.Tick.Server]
        private void UpdateBalance()
        {
            // Sync balance from projection to networked property
            Balance = _walletService.GetBalance(Entity.SteamId);
        }

        public async Task<bool> TransferTo(Player recipient, long amount, string reason)
        {
            var result = await _walletService.TransferMoney(
                Entity.SteamId,
                recipient.SteamId,
                amount,
                reason
            );

            return result.IsSuccess;
        }
    }
}
```

### Mining System Integration

```csharp
using Sandbox;
using GameRP.Economy.FederalReserve.Commands;

namespace GameRP.Economy.SandboxIntegration
{
    public class MiningSystem
    {
        private readonly ICommandHandler<DepositGold> _depositGoldHandler;

        public async Task<bool> SellGoldToFederalReserve(Player player, int goldBars)
        {
            var command = new DepositGold
            {
                PlayerSteamId = player.SteamId,
                GoldBars = goldBars
            };

            var result = await _depositGoldHandler.HandleAsync(command);

            if (result.IsSuccess)
            {
                // Remove gold from player inventory
                player.Inventory.Remove("gold_bar", goldBars);

                Log.Info($"Player {player.SteamId} sold {goldBars} gold bars");
                return true;
            }

            return false;
        }
    }
}
```

---

## Persistence

### Event Store Implementation (SQLite)

```csharp
namespace GameRP.Economy.Infrastructure
{
    using System.Data.SQLite;
    using GameRP.Economy.Core;
    using Newtonsoft.Json;

    public class SQLiteEventStore : IEventStore
    {
        private readonly string _connectionString;

        public SQLiteEventStore(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Events (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    EventId TEXT NOT NULL UNIQUE,
                    AggregateId TEXT NOT NULL,
                    Version INTEGER NOT NULL,
                    EventType TEXT NOT NULL,
                    EventData TEXT NOT NULL,
                    Timestamp TEXT NOT NULL,
                    UNIQUE(AggregateId, Version)
                );

                CREATE INDEX IF NOT EXISTS IX_Events_AggregateId ON Events(AggregateId);
                CREATE INDEX IF NOT EXISTS IX_Events_EventType ON Events(EventType);
                CREATE INDEX IF NOT EXISTS IX_Events_Timestamp ON Events(Timestamp);
            ";
            command.ExecuteNonQuery();
        }

        public async Task AppendEventsAsync(string aggregateId, IEnumerable<IEvent> events, long expectedVersion)
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var @event in events)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO Events (EventId, AggregateId, Version, EventType, EventData, Timestamp)
                        VALUES (@EventId, @AggregateId, @Version, @EventType, @EventData, @Timestamp)
                    ";

                    command.Parameters.AddWithValue("@EventId", @event.EventId.ToString());
                    command.Parameters.AddWithValue("@AggregateId", @event.AggregateId);
                    command.Parameters.AddWithValue("@Version", @event.Version);
                    command.Parameters.AddWithValue("@EventType", @event.GetType().AssemblyQualifiedName);
                    command.Parameters.AddWithValue("@EventData", JsonConvert.SerializeObject(@event));
                    command.Parameters.AddWithValue("@Timestamp", @event.Timestamp.ToString("O"));

                    await command.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<IEvent>> GetEventsAsync(string aggregateId)
        {
            var events = new List<IEvent>();

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT EventType, EventData
                FROM Events
                WHERE AggregateId = @AggregateId
                ORDER BY Version ASC
            ";
            command.Parameters.AddWithValue("@AggregateId", aggregateId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var eventType = Type.GetType(reader.GetString(0));
                var eventData = reader.GetString(1);
                var @event = JsonConvert.DeserializeObject(eventData, eventType) as IEvent;
                events.Add(@event);
            }

            return events;
        }

        public async Task<IEnumerable<TEvent>> GetEventsByTypeAsync<TEvent>() where TEvent : IEvent
        {
            var events = new List<TEvent>();
            var eventType = typeof(TEvent).AssemblyQualifiedName;

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT EventData
                FROM Events
                WHERE EventType = @EventType
                ORDER BY Timestamp ASC
            ";
            command.Parameters.AddWithValue("@EventType", eventType);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var eventData = reader.GetString(0);
                var @event = JsonConvert.DeserializeObject<TEvent>(eventData);
                events.Add(@event);
            }

            return events;
        }

        public async Task<IEnumerable<IEvent>> GetEventsSinceVersionAsync(long version)
        {
            var events = new List<IEvent>();

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT EventType, EventData
                FROM Events
                WHERE Id > @Version
                ORDER BY Id ASC
            ";
            command.Parameters.AddWithValue("@Version", version);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var eventType = Type.GetType(reader.GetString(0));
                var eventData = reader.GetString(1);
                var @event = JsonConvert.DeserializeObject(eventData, eventType) as IEvent;
                events.Add(@event);
            }

            return events;
        }
    }
}
```

---

## Dependency Injection Setup

```csharp
namespace GameRP.Economy
{
    using Microsoft.Extensions.DependencyInjection;
    using GameRP.Economy.Core;
    using GameRP.Economy.Services;
    using GameRP.Economy.Wallet;
    using GameRP.Economy.Wallet.CommandHandlers;
    using GameRP.Economy.Projections;

    public static class ServiceConfiguration
    {
        public static IServiceCollection AddEconomySystem(this IServiceCollection services, string dbPath)
        {
            // Core infrastructure
            services.AddSingleton<IEventStore>(new SQLiteEventStore(dbPath));
            services.AddSingleton<IEventBus, InMemoryEventBus>();
            services.AddSingleton<IRateLimiter, RateLimiter>();

            // Repositories
            services.AddScoped(typeof(IRepository<>), typeof(EventSourcedRepository<>));

            // Command handlers
            services.AddScoped<ICommandHandler<CreateWallet>, CreateWalletHandler>();
            services.AddScoped<ICommandHandler<TransferMoney>, TransferMoneyHandler>();
            // ... register all command handlers

            // Projections
            var walletProjection = new WalletBalanceProjection();
            var economicStatsProjection = new EconomicStatsProjection();

            services.AddSingleton(walletProjection);
            services.AddSingleton(economicStatsProjection);

            // Subscribe projections to events
            var eventBus = services.BuildServiceProvider().GetService<IEventBus>();
            eventBus.Subscribe(walletProjection);
            eventBus.Subscribe(economicStatsProjection);

            // Services
            services.AddScoped<WalletService>();
            services.AddScoped<BankingService>();
            services.AddScoped<GovernmentService>();

            return services;
        }
    }
}
```

---

## Usage Example

```csharp
// Initialize system
var services = new ServiceCollection();
services.AddEconomySystem("economy.db");
var serviceProvider = services.BuildServiceProvider();

// Create wallet for new player
var walletService = serviceProvider.GetService<WalletService>();
await walletService.CreateWalletForPlayer(steamId: 76561198012345678);

// Transfer money
var result = await walletService.TransferMoney(
    fromSteamId: 76561198012345678,
    toSteamId: 76561198087654321,
    amount: 5000,
    reason: "Payment for services"
);

if (result.IsSuccess)
{
    Log.Info("Transfer successful!");
}

// Check balance (from projection - fast!)
long balance = walletService.GetBalance(76561198012345678);
Log.Info($"Balance: ${balance}");
```

---

## Benefits of This Architecture

1. **Complete Audit Trail**: Every change is recorded as an event
2. **Debugging**: Can replay events to see exactly what happened
3. **Time Travel**: Can see state at any point in time
4. **Performance**: Projections provide fast read access
5. **Scalability**: Write and read models can be scaled independently
6. **Flexibility**: Can create new projections from existing events
7. **Security**: Immutable event log prevents tampering
8. **Testing**: Easy to test by replaying events

---

## Summary

This event-sourced architecture provides a robust, auditable foundation for the economy system. All state changes are captured as events, enabling complete transaction history, powerful debugging capabilities, and flexible read models optimized for different use cases.
