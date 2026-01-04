# Implementation Guide

## Overview

This guide provides a roadmap for implementing the gold-backed economy system in S&Box. Implementation is divided into phases to ensure stability and proper testing.

## Technology Stack

### S&Box Framework
- **Game Engine:** S&Box (Source 2)
- **Language:** C# (.NET)
- **Networking:** Built-in S&Box networking
- **Database:** MySQL/PostgreSQL or S&Box persistent storage
- **UI:** Razor (S&Box UI framework)

### Required Systems
- Entity system
- Inventory system
- NPC system
- UI/HUD system
- Networking/replication
- Database persistence

---

## Implementation Phases

### Phase 1: Foundation (Week 1-2)
**Core Money System**

**Goals:**
- Establish basic currency framework
- Implement Federal Reserve
- Create gold mining mechanics
- Basic transaction system

**Tasks:**

**1.1 Currency Class**
```csharp
public class Currency {
    public long Amount { get; set; }

    public static Currency operator +(Currency a, Currency b)
    public static Currency operator -(Currency a, Currency b)
    public static bool CanAfford(Currency balance, Currency cost)
    public string FormatCurrency() // Returns "$1,000"
}
```

**1.2 Player Money Component**
```csharp
public class PlayerWallet : EntityComponent {
    [Net] public long Balance { get; set; }

    [Server]
    public bool AddMoney(long amount, string source)

    [Server]
    public bool RemoveMoney(long amount, string reason)

    [Server]
    public bool TransferTo(Player recipient, long amount)
}
```

**1.3 Federal Reserve Entity**
```csharp
public class FederalReserve : Entity {
    public static long TotalGoldReserves { get; set; }
    public static long TotalCurrencyInCirculation { get; set; }
    public static float ExchangeRate { get; set; } = 1000f;

    [Server]
    public static void DepositGold(Player player, int goldBars)

    [Server]
    public static void WithdrawGold(Player player, long currency)

    public static void UpdateEconomicData()
}
```

**1.4 Gold Item**
```csharp
public class GoldBar : Entity, ICarriable {
    [Net] public string UniqueID { get; set; }
    [Net] public long CreatedTimestamp { get; set; }
    [Net] public long CreatorSteamID { get; set; }

    public override void Spawn() {
        UniqueID = GenerateUniqueID();
        CreatedTimestamp = DateTime.UtcNow.Ticks;
    }
}
```

**1.5 Gold Mining System**
```csharp
public class GoldOreNode : Entity {
    [Net] public int OreAmount { get; set; } = 10;
    [Net] public float RespawnTime { get; set; } = 300f;
    [Net] public bool IsDepleted { get; set; }

    [Server]
    public void Mine(Player player) {
        if (IsDepleted) return;

        // Give ore to player
        player.Inventory.Add(new GoldOre());
        OreAmount--;

        if (OreAmount <= 0) {
            IsDepleted = true;
            RespawnTimer();
        }
    }
}
```

**1.6 Transaction Logger**
```csharp
public static class TransactionLogger {
    public static void LogTransaction(
        long fromSteamID,
        long toSteamID,
        long amount,
        string transactionType,
        string details
    ) {
        // Log to database
        var transaction = new Transaction {
            From = fromSteamID,
            To = toSteamID,
            Amount = amount,
            Type = transactionType,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        Database.Insert(transaction);
    }
}
```

**Deliverables:**
- Players can mine gold ore
- Players can smelt ore into gold bars
- Players can sell gold to Federal Reserve for currency
- Players can hold currency (balance visible)
- Basic transactions (player to player)
- Transaction logging

**Testing:**
- Mine gold successfully
- Sell to Federal Reserve
- Receive correct currency amount
- Transfer between players
- Verify logging works

---

### Phase 2: Taxation (Week 3)
**Basic Tax System**

**Goals:**
- Implement income tax
- Implement sales tax
- Create government treasury
- Automatic tax collection

**Tasks:**

**2.1 Government Treasury**
```csharp
public static class Government {
    public static long TreasuryBalance { get; set; }

    [Server]
    public static void CollectTax(long amount, string taxType) {
        TreasuryBalance += amount;
        TransactionLogger.LogTax(amount, taxType);
    }

    [Server]
    public static bool SpendFunds(long amount, string purpose) {
        if (TreasuryBalance >= amount) {
            TreasuryBalance -= amount;
            TransactionLogger.LogGovernmentSpending(amount, purpose);
            return true;
        }
        return false;
    }
}
```

**2.2 Tax Calculator**
```csharp
public static class TaxCalculator {
    public static long CalculateIncomeTax(long income) {
        if (income < 50000) return (long)(income * 0.15f);
        if (income < 200000) return (long)(income * 0.20f);
        return (long)(income * 0.25f);
    }

    public static long CalculateSalesTax(long amount) {
        return (long)(amount * 0.07f); // 7% sales tax
    }
}
```

**2.3 Automatic Tax Withholding**
```csharp
public static class Payroll {
    [Server]
    public static void PaySalary(Player employee, long grossAmount) {
        long tax = TaxCalculator.CalculateIncomeTax(grossAmount);
        long netAmount = grossAmount - tax;

        employee.Wallet.AddMoney(netAmount, "Salary");
        Government.CollectTax(tax, "Income Tax");

        // Log
        TransactionLogger.LogPayroll(employee, grossAmount, tax, netAmount);
    }
}
```

**2.4 Sales Tax on Purchases**
```csharp
public class Shop : Entity {
    [Server]
    public bool PurchaseItem(Player player, Item item, long basePrice) {
        long salesTax = TaxCalculator.CalculateSalesTax(basePrice);
        long totalPrice = basePrice + salesTax;

        if (player.Wallet.Balance >= totalPrice) {
            player.Wallet.RemoveMoney(totalPrice, "Purchase");
            Government.CollectTax(salesTax, "Sales Tax");
            player.Inventory.Add(item);

            // Shop keeps base price
            return true;
        }
        return false;
    }
}
```

**Deliverables:**
- Income tax automatically collected on salaries
- Sales tax added to purchases
- Government treasury tracks balance
- Tax revenue logged

**Testing:**
- Receive salary, verify tax withheld
- Purchase item, verify sales tax charged
- Check government treasury balance
- Verify transaction logs

---

### Phase 3: Jobs & Salaries (Week 4)
**Government Jobs**

**Goals:**
- Implement public sector jobs
- Automatic salary payments
- Clock in/out system
- Job requirements

**Tasks:**

**3.1 Job System**
```csharp
public enum JobType {
    Unemployed,
    Miner,
    Police,
    EMS,
    Firefighter,
    // etc.
}

public class JobComponent : EntityComponent {
    [Net] public JobType CurrentJob { get; set; }
    [Net] public bool IsClockedIn { get; set; }
    [Net] public DateTime ClockInTime { get; set; }

    [Server]
    public void ClockIn() {
        if (CurrentJob == JobType.Unemployed) return;

        IsClockedIn = true;
        ClockInTime = DateTime.UtcNow;
    }

    [Server]
    public void ClockOut() {
        if (!IsClockedIn) return;

        TimeSpan worked = DateTime.UtcNow - ClockInTime;
        PaySalary(worked);
        IsClockedIn = false;
    }

    private void PaySalary(TimeSpan worked) {
        long hourlyRate = GetJobSalary(CurrentJob);
        long grossPay = (long)(worked.TotalHours * hourlyRate);
        Payroll.PaySalary(Entity as Player, grossPay);
    }
}
```

**3.2 Job Salary Rates**
```csharp
public static class JobSalaries {
    public static Dictionary<JobType, long> Rates = new() {
        { JobType.Police, 1500 },
        { JobType.EMS, 1000 },
        { JobType.Firefighter, 1200 },
        { JobType.CityWorker, 800 },
        // etc.
    };
}
```

**3.3 Job Selection UI**
```razor
<div class="job-selection">
    <h2>Available Jobs</h2>
    @foreach (var job in AvailableJobs) {
        <div class="job-card">
            <h3>@job.Name</h3>
            <p>Salary: $@job.HourlyRate/hour</p>
            <button onclick="@(() => SelectJob(job))">Take Job</button>
        </div>
    }
</div>
```

**Deliverables:**
- Players can select jobs
- Clock in/out system
- Automatic salary calculation
- Government pays salaries from treasury

**Testing:**
- Take police job
- Clock in for 1 hour
- Clock out, verify salary received
- Verify tax withheld
- Check government treasury reduced

---

### Phase 4: Banking System (Week 5-6)
**Player Banks**

**Goals:**
- Player can create banks
- Deposit/withdrawal system
- Loan system
- Reserve requirements

**Tasks:**

**4.1 Bank Entity**
```csharp
public class Bank : Entity {
    [Net] public long OwnerSteamID { get; set; }
    [Net] public string BankName { get; set; }
    [Net] public long TotalDeposits { get; set; }
    [Net] public long TotalLoans { get; set; }
    [Net] public long Reserves { get; set; }
    [Net] public float DepositInterestRate { get; set; } = 0.02f;
    [Net] public float LoanInterestRate { get; set; } = 0.08f;

    public float ReserveRatio => (float)Reserves / TotalDeposits;

    [Server]
    public bool AcceptDeposit(Player player, long amount) {
        if (player.Wallet.Balance < amount) return false;

        player.Wallet.RemoveMoney(amount, "Bank Deposit");
        TotalDeposits += amount;
        Reserves += amount;

        CreateAccount(player, amount);
        return true;
    }

    [Server]
    public bool ProcessWithdrawal(Player player, long amount) {
        var account = GetAccount(player);
        if (account == null || account.Balance < amount) return false;
        if (Reserves < amount) return false; // Bank run!

        account.Balance -= amount;
        TotalDeposits -= amount;
        Reserves -= amount;
        player.Wallet.AddMoney(amount, "Bank Withdrawal");

        return true;
    }

    [Server]
    public bool IssueLoan(Player borrower, long amount, int termWeeks) {
        if (ReserveRatio < 0.10f) return false; // Reserve requirement

        // Create loan
        var loan = new Loan {
            BankID = NetworkIdent,
            BorrowerID = borrower.SteamId,
            Amount = amount,
            InterestRate = LoanInterestRate,
            TermWeeks = termWeeks
        };

        borrower.Wallet.AddMoney(amount, "Bank Loan");
        TotalLoans += amount;
        Reserves -= amount;

        Database.Insert(loan);
        return true;
    }
}
```

**4.2 Bank Account**
```csharp
public class BankAccount {
    public long BankID { get; set; }
    public long PlayerSteamID { get; set; }
    public long Balance { get; set; }
    public float InterestRate { get; set; }
    public DateTime OpenedDate { get; set; }
}
```

**4.3 Loan System**
```csharp
public class Loan {
    public long LoanID { get; set; }
    public long BankID { get; set; }
    public long BorrowerID { get; set; }
    public long Amount { get; set; }
    public float InterestRate { get; set; }
    public int TermWeeks { get; set; }
    public long RemainingBalance { get; set; }
    public DateTime NextPaymentDue { get; set; }
    public LoanStatus Status { get; set; }
}

public enum LoanStatus {
    Active,
    PaidOff,
    Defaulted
}
```

**Deliverables:**
- Players can create banks (with capital requirement)
- Deposit/withdrawal system
- Interest accrual
- Loan issuance
- Reserve ratio enforcement

**Testing:**
- Create bank with $100,000
- Deposit $50,000 from another player
- Issue loan for $40,000
- Verify reserve ratio maintained
- Process withdrawal

---

### Phase 5: Property Tax & Advanced Taxes (Week 7)
**Complete Tax System**

**Goals:**
- Property tax system
- Business license fees
- Vehicle registration
- Estate tax (death)

**Tasks:**

**5.1 Property System**
```csharp
public class Property : Entity {
    [Net] public long OwnerSteamID { get; set; }
    [Net] public long AssessedValue { get; set; }
    [Net] public DateTime LastTaxPayment { get; set; }
    [Net] public long TaxOwed { get; set; }

    [GameLoop]
    public void CalculatePropertyTax() {
        // Weekly property tax (2% annually = 0.038% weekly)
        long weeklyTax = (long)(AssessedValue * 0.00038f);
        TaxOwed += weeklyTax;

        // Auto-pay if possible
        var owner = GetOwnerPlayer();
        if (owner != null && owner.Wallet.Balance >= TaxOwed) {
            owner.Wallet.RemoveMoney(TaxOwed, "Property Tax");
            Government.CollectTax(TaxOwed, "Property Tax");
            TaxOwed = 0;
            LastTaxPayment = DateTime.UtcNow;
        }
    }
}
```

**5.2 Business License**
```csharp
public class Business : Entity {
    [Net] public long OwnerSteamID { get; set; }
    [Net] public BusinessType Type { get; set; }
    [Net] public DateTime LicenseExpiration { get; set; }
    [Net] public bool IsLicenseValid => DateTime.UtcNow < LicenseExpiration;

    [Server]
    public bool RenewLicense() {
        long fee = GetLicenseFee(Type);
        var owner = GetOwnerPlayer();

        if (owner.Wallet.Balance >= fee) {
            owner.Wallet.RemoveMoney(fee, "Business License");
            Government.CollectTax(fee, "Business License");
            LicenseExpiration = DateTime.UtcNow.AddMonths(1);
            return true;
        }
        return false;
    }
}
```

**5.3 Estate Tax (Death)**
```csharp
public class DeathTaxHandler {
    [Server]
    public static void OnPlayerDeath(Player player) {
        long totalWealth = CalculateTotalWealth(player);
        long estateTax = (long)(totalWealth * 0.10f); // 10%

        // Deduct from balance
        long taxPaid = Math.Min(player.Wallet.Balance, estateTax);
        player.Wallet.RemoveMoney(taxPaid, "Estate Tax");
        Government.CollectTax(taxPaid, "Estate Tax");

        // Log money removed from economy
        FederalReserve.TotalCurrencyInCirculation -= taxPaid;
    }

    private static long CalculateTotalWealth(Player player) {
        long cash = player.Wallet.Balance;
        long bankDeposits = GetTotalBankDeposits(player);
        long propertyValue = GetTotalPropertyValue(player);
        long vehicleValue = GetTotalVehicleValue(player);

        return cash + bankDeposits + propertyValue + vehicleValue;
    }
}
```

**Deliverables:**
- Automated property tax collection
- Business license renewal system
- Estate tax on death
- Complete tax framework

---

### Phase 6: UI & UX (Week 8)
**Player Interfaces**

**Goals:**
- Banking UI
- Government budget dashboard
- Personal finance dashboard
- Transaction history

**Tasks:**

**6.1 Personal Finance Dashboard**
```razor
<div class="finance-dashboard">
    <div class="balance-card">
        <h2>Wallet Balance</h2>
        <p class="amount">$@Player.Wallet.Balance.ToString("N0")</p>
    </div>

    <div class="income-card">
        <h3>Weekly Income</h3>
        <p>$@WeeklyIncome.ToString("N0")</p>
    </div>

    <div class="taxes-card">
        <h3>Taxes Paid This Week</h3>
        <p>$@TaxesPaid.ToString("N0")</p>
    </div>

    <div class="transactions">
        <h3>Recent Transactions</h3>
        @foreach (var txn in RecentTransactions) {
            <div class="transaction-row">
                <span>@txn.Description</span>
                <span class="@(txn.Amount > 0 ? "positive" : "negative")">
                    $@txn.Amount.ToString("N0")
                </span>
            </div>
        }
    </div>
</div>
```

**6.2 Banking Interface**
```razor
<div class="bank-interface">
    <h2>@BankName</h2>

    <div class="account-info">
        <p>Account Balance: $@AccountBalance.ToString("N0")</p>
        <p>Interest Rate: @InterestRate%</p>
    </div>

    <div class="actions">
        <button onclick="@ShowDeposit">Deposit</button>
        <button onclick="@ShowWithdraw">Withdraw</button>
        <button onclick="@ShowLoanRequest">Request Loan</button>
    </div>
</div>
```

**6.3 Federal Reserve Dashboard (Public)**
```razor
<div class="federal-reserve-dashboard">
    <h2>Federal Reserve Economic Data</h2>

    <div class="stat">
        <label>Total Gold Reserves:</label>
        <span>@FederalReserve.TotalGoldReserves bars</span>
    </div>

    <div class="stat">
        <label>Total Currency in Circulation:</label>
        <span>$@FederalReserve.TotalCurrencyInCirculation.ToString("N0")</span>
    </div>

    <div class="stat">
        <label>Exchange Rate:</label>
        <span>1 gold bar = $@FederalReserve.ExchangeRate.ToString("N0")</span>
    </div>
</div>
```

**Deliverables:**
- Clean, functional UIs for all systems
- Real-time updates
- Mobile-friendly (if applicable)

---

### Phase 7: Anti-Exploit & Security (Week 9)
**Security Hardening**

**Goals:**
- Transaction validation
- Rate limiting
- Exploit detection
- Admin tools

**Tasks:**

**7.1 Rate Limiting**
```csharp
public class RateLimiter {
    private static Dictionary<long, Queue<DateTime>> playerActions = new();

    public static bool CheckRateLimit(long steamID, string action, int maxPerMinute) {
        if (!playerActions.ContainsKey(steamID)) {
            playerActions[steamID] = new Queue<DateTime>();
        }

        var queue = playerActions[steamID];
        var now = DateTime.UtcNow;

        // Remove actions older than 1 minute
        while (queue.Count > 0 && (now - queue.Peek()).TotalMinutes > 1) {
            queue.Dequeue();
        }

        if (queue.Count >= maxPerMinute) {
            return false; // Rate limit exceeded
        }

        queue.Enqueue(now);
        return true;
    }
}
```

**7.2 Transaction Validation**
```csharp
public static class TransactionValidator {
    [Server]
    public static bool ValidateTransaction(Player from, Player to, long amount) {
        // Check amount
        if (amount <= 0) {
            Log.Warning($"Invalid amount: {amount}");
            return false;
        }

        // Check balance
        if (from.Wallet.Balance < amount) {
            Log.Warning($"Insufficient balance: {from.SteamId}");
            return false;
        }

        // Check rate limit
        if (!RateLimiter.CheckRateLimit(from.SteamId, "transfer", 10)) {
            Log.Warning($"Rate limit exceeded: {from.SteamId}");
            return false;
        }

        // Check max transaction
        if (amount > 1000000) {
            Log.Info($"Large transaction flagged: {amount}");
            // Allow but log for review
        }

        return true;
    }
}
```

**7.3 Admin Tools**
```csharp
[ConCmd.Admin("eco_inspect")]
public static void InspectPlayer(long steamID) {
    var data = Database.GetPlayerEconomicData(steamID);

    Log.Info($"=== Economic Inspection: {steamID} ===");
    Log.Info($"Balance: ${data.Balance}");
    Log.Info($"Total Earned: ${data.TotalEarned}");
    Log.Info($"Total Spent: ${data.TotalSpent}");
    Log.Info($"Owned Properties: {data.PropertyCount}");
    Log.Info($"Active Loans: {data.LoanCount}");
    Log.Info($"Recent Transactions: {data.RecentTransactions.Count}");
}

[ConCmd.Admin("eco_rollback")]
public static void RollbackTransaction(string transactionID) {
    var txn = Database.GetTransaction(transactionID);
    if (txn == null) return;

    // Reverse transaction
    var from = GetPlayerBySteamID(txn.FromSteamID);
    var to = GetPlayerBySteamID(txn.ToSteamID);

    to.Wallet.RemoveMoney(txn.Amount, "Rollback");
    from.Wallet.AddMoney(txn.Amount, "Rollback");

    TransactionLogger.LogRollback(transactionID);
    Log.Info($"Transaction {transactionID} rolled back");
}
```

**Deliverables:**
- All transactions validated server-side
- Rate limiting on all money operations
- Admin investigation commands
- Automated suspicious activity detection

---

### Phase 8: Balancing & Testing (Week 10+)
**Economy Tuning**

**Goals:**
- Balance earning rates
- Balance tax rates
- Test edge cases
- Optimize performance
- Beta test with players

**Tasks:**

**8.1 Economic Simulator**
```csharp
public class EconomySimulator {
    public static void SimulateWeek(int playerCount) {
        // Simulate player behavior
        for (int day = 0; day < 7; day++) {
            // Mining
            long goldMined = SimulateMiners(playerCount);
            long moneyCreated = goldMined * 1000;

            // Spending
            long moneySpent = (long)(moneyCreated * 0.8f);

            // Taxes
            long taxCollected = (long)(moneySpent * 0.15f);

            // Government spending
            long govSpending = taxCollected;

            Log.Info($"Day {day}: Created ${moneyCreated}, Spent ${moneySpent}, Tax ${taxCollected}");
        }
    }
}
```

**8.2 Balance Adjustments**
- Mining rates
- Salary levels
- Tax percentages
- Gold exchange rate
- Reserve requirements

**8.3 Beta Testing Checklist**
```
[ ] 10 players can sustain economy
[ ] 50 players stress test
[ ] 100 players performance
[ ] No exploits found
[ ] Inflation under control
[ ] Players find it fun
[ ] Government budget stable
[ ] Banks functioning properly
```

**Deliverables:**
- Balanced, stable economy
- No major exploits
- Good performance
- Positive player feedback

---

## Database Schema

### MySQL Example

```sql
-- Players
CREATE TABLE players (
    steam_id BIGINT PRIMARY KEY,
    balance BIGINT NOT NULL DEFAULT 0,
    total_earned BIGINT DEFAULT 0,
    total_spent BIGINT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT positive_balance CHECK (balance >= 0)
);

-- Transactions
CREATE TABLE transactions (
    id VARCHAR(64) PRIMARY KEY,
    from_steam_id BIGINT,
    to_steam_id BIGINT,
    amount BIGINT NOT NULL,
    transaction_type VARCHAR(50),
    description TEXT,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_from (from_steam_id),
    INDEX idx_to (to_steam_id),
    INDEX idx_timestamp (timestamp)
);

-- Banks
CREATE TABLE banks (
    id INT PRIMARY KEY AUTO_INCREMENT,
    owner_steam_id BIGINT NOT NULL,
    name VARCHAR(100),
    total_deposits BIGINT DEFAULT 0,
    total_loans BIGINT DEFAULT 0,
    reserves BIGINT DEFAULT 0,
    deposit_rate FLOAT DEFAULT 0.02,
    loan_rate FLOAT DEFAULT 0.08,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Bank Accounts
CREATE TABLE bank_accounts (
    id INT PRIMARY KEY AUTO_INCREMENT,
    bank_id INT,
    player_steam_id BIGINT,
    balance BIGINT DEFAULT 0,
    interest_rate FLOAT,
    opened_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (bank_id) REFERENCES banks(id)
);

-- Loans
CREATE TABLE loans (
    id INT PRIMARY KEY AUTO_INCREMENT,
    bank_id INT,
    borrower_steam_id BIGINT,
    amount BIGINT NOT NULL,
    interest_rate FLOAT,
    term_weeks INT,
    remaining_balance BIGINT,
    next_payment_due TIMESTAMP,
    status ENUM('active', 'paid_off', 'defaulted'),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (bank_id) REFERENCES banks(id)
);

-- Properties
CREATE TABLE properties (
    id INT PRIMARY KEY AUTO_INCREMENT,
    owner_steam_id BIGINT,
    assessed_value BIGINT,
    tax_owed BIGINT DEFAULT 0,
    last_tax_payment TIMESTAMP,
    location_x FLOAT,
    location_y FLOAT,
    location_z FLOAT
);

-- Government Data
CREATE TABLE government (
    id INT PRIMARY KEY DEFAULT 1,
    treasury_balance BIGINT DEFAULT 0,
    total_revenue_collected BIGINT DEFAULT 0,
    total_spending BIGINT DEFAULT 0
);

-- Federal Reserve
CREATE TABLE federal_reserve (
    id INT PRIMARY KEY DEFAULT 1,
    total_gold_reserves BIGINT DEFAULT 0,
    total_currency_in_circulation BIGINT DEFAULT 0,
    exchange_rate FLOAT DEFAULT 1000.0
);
```

---

## Performance Considerations

### Optimization Tips

**Database:**
- Index frequently queried columns (steam_id, timestamp)
- Use connection pooling
- Batch insert transactions
- Periodic cleanup of old records

**Networking:**
- Only sync essential data to clients
- Use delta compression
- Batch updates where possible

**Server:**
- Process taxes/interest in batches (not per-player loop)
- Cache frequently accessed data
- Use async database operations
- Optimize hot paths (transactions)

**Memory:**
- Don't store full transaction history in memory
- Lazy-load data as needed
- Periodic garbage collection

---

## Testing Strategy

### Unit Tests
```csharp
[Test]
public void TestMoneyTransfer() {
    var playerA = CreateTestPlayer(1000);
    var playerB = CreateTestPlayer(500);

    bool success = playerA.Wallet.TransferTo(playerB, 300);

    Assert.IsTrue(success);
    Assert.AreEqual(700, playerA.Wallet.Balance);
    Assert.AreEqual(800, playerB.Wallet.Balance);
}

[Test]
public void TestInsufficientFunds() {
    var player = CreateTestPlayer(100);
    bool success = player.Wallet.RemoveMoney(200, "Test");

    Assert.IsFalse(success);
    Assert.AreEqual(100, player.Wallet.Balance);
}
```

### Integration Tests
- Full transaction flows
- Tax collection pipeline
- Bank operations
- Government budget cycle

### Stress Tests
- 100 players mining simultaneously
- 1000 transactions per second
- Bank run scenario
- Economic crisis simulation

---

## Launch Checklist

### Pre-Launch
```
[ ] All Phase 1-7 features implemented
[ ] Database schema deployed
[ ] Admin tools functional
[ ] Security measures in place
[ ] Balance testing complete
[ ] Performance optimized
[ ] Documentation written
[ ] Tutorial/guide for players
[ ] Beta test completed
[ ] Backup/recovery plan
```

### Launch Day
```
[ ] Database initialized
[ ] Federal Reserve at 0 gold, 0 currency
[ ] Gold nodes spawned
[ ] Government initialized
[ ] Monitoring dashboard active
[ ] Admin team ready
[ ] Player support ready
```

### Post-Launch
```
[ ] Monitor for exploits
[ ] Track economic metrics
[ ] Gather player feedback
[ ] Adjust balance as needed
[ ] Fix bugs promptly
[ ] Regular economic reports
```

---

## Maintenance & Updates

### Weekly Tasks
- Review transaction logs
- Check for exploits
- Monitor money supply
- Adjust balance if needed
- Review player reports

### Monthly Tasks
- Economic health report
- Security audit
- Performance review
- Player satisfaction survey
- Balance patch if needed

### Ongoing
- Fix bugs
- Add features based on feedback
- Optimize performance
- Update documentation
- Community engagement

---

## Resources & Tools

### Development Tools
- Visual Studio / Rider (C# IDE)
- S&Box SDK
- Database client (MySQL Workbench, pgAdmin)
- Git for version control

### Monitoring Tools
- Custom admin dashboard
- Database query tools
- Log aggregation
- Performance profilers

### Community Resources
- S&Box documentation: https://docs.facepunch.com/s/sbox-dev/
- Discord for support
- Economy design references (EVE Online, etc.)

---

## Support & Documentation

### Player Documentation
- How to mine gold
- How to use banks
- Tax system explained
- Job guide
- FAQ

### Admin Documentation
- Server setup
- Database configuration
- Admin commands
- Troubleshooting
- Balance guide

### Developer Documentation
- API reference
- Code architecture
- Database schema
- Extending the system
- Contributing guidelines
