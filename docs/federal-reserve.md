# Federal Reserve System

## Overview

The Federal Reserve is the central banking authority that controls the money supply. It is the **ONLY** entity that can create or destroy currency in the economy.

## Core Functions

### 1. Currency Issuance
- Issues currency only when gold is deposited
- Maintains strict gold-to-currency ratio
- Default: 1 gold bar = $1,000 (configurable)
- Tracks total currency in circulation

### 2. Gold Reserve Management
- Stores all gold backing the currency
- Displays current gold reserves
- Displays total currency in circulation
- Ensures 100% backing at all times

### 3. Monetary Policy
- Sets interest rates for bank borrowing
- Controls reserve requirements for banks
- Adjusts gold-to-currency ratio if needed

### 4. Lender of Last Resort
- Banks can borrow from Federal Reserve
- Charges interest on loans to banks
- Prevents banking system collapse

### 5. Currency Exchange
- Buys currency back from players for gold (deflationary)
- Destroys currency when exchanged for gold
- Removes gold from reserves

## Mechanics

### Selling Gold for Currency
```
Player brings gold bars to Federal Reserve
→ Federal Reserve accepts gold
→ Adds gold to reserves
→ Creates currency (ratio-based)
→ Gives currency to player
```

**Example:**
- Player has 5 gold bars
- Exchange rate: 1 gold = $1,000
- Player receives $5,000
- Federal Reserve reserves: +5 gold bars
- Total currency in circulation: +$5,000

### Buying Gold with Currency
```
Player brings currency to Federal Reserve
→ Federal Reserve accepts currency
→ Removes currency from circulation (destroy)
→ Removes gold from reserves
→ Gives gold to player
```

**Example:**
- Player has $5,000
- Exchange rate: 1 gold = $1,000
- Player receives 5 gold bars
- Federal Reserve reserves: -5 gold bars
- Total currency in circulation: -$5,000

### Bank Borrowing
```
Bank needs reserves to meet requirements
→ Bank requests loan from Federal Reserve
→ Federal Reserve issues currency loan
→ Bank must repay with interest (e.g., 3%)
→ Interest is destroyed (deflationary)
```

## Dashboard Displays

### Public Information
- Total gold reserves
- Total currency in circulation
- Current gold-to-currency ratio
- Current interest rate for bank loans

### Admin Information
- Historical gold deposits
- Historical currency creation
- Bank borrowing activity
- Economic health indicators

## Economic Controls

### Adjustable Parameters
- **Gold-to-currency ratio**: Controls how much money each gold bar creates
- **Bank loan interest rate**: Controls cost of borrowing for banks
- **Reserve requirements**: Controls how much banks must hold
- **Exchange fees**: Small fee to discourage speculation (optional)

### Inflation Control
If too much gold enters the economy:
- Option 1: Decrease gold-to-currency ratio (each gold = less money)
- Option 2: Increase mining difficulty
- Option 3: Add gold sinks (government purchases, crafting)

### Deflation Control
If currency supply is too tight:
- Option 1: Increase gold-to-currency ratio
- Option 2: Decrease mining difficulty
- Option 3: Federal Reserve buys goods/services (inject money)

## Location & Access

### Physical Location
- Prominent building on the map
- Secure vault (visual representation)
- NPC tellers or interactive terminals
- Display screens showing economic data

### Access Control
- **Gold exchange**: Open to all players
- **Bank borrowing**: Restricted to registered banks
- **Monetary policy**: Admin/government only
- **Vault access**: No player access

## Anti-Exploit Features

### Transaction Limits
- Rate limiting on gold sales (prevent spam)
- Minimum transaction amounts
- Cooldowns on large transactions

### Audit Trail
- Log all gold deposits
- Log all currency creation/destruction
- Track who deposited what and when
- Admin tools to review activity

### Security
- Cannot be robbed by players (unless designed as event)
- Gold reserves cannot be stolen
- Currency cannot be duplicated
- All transactions are atomic (no partial failures)

## Relationship to Other Systems

### With Mining
- Miners sell gold here to get paid
- Mining rate controls money supply growth
- Federal Reserve data shows mining activity

### With Banks
- Banks borrow when reserves are low
- Federal Reserve sets borrowing costs
- Can monitor bank health

### With Government
- Government can borrow for deficit spending
- Government pays interest on loans
- Federal Reserve is independent (not controlled by government)

### With Economy
- Controls inflation/deflation
- Enables economic growth through money supply
- Provides stability and trust in currency

## Implementation Notes

### Database Fields
```
gold_reserves: integer (total gold bars)
currency_in_circulation: integer (total dollars)
exchange_rate: float (default: 1000.0)
bank_loan_rate: float (default: 0.03)
reserve_requirement: float (default: 0.10)
```

### Key Functions
```
DepositGold(player, amount)
WithdrawGold(player, amount)
BankLoan(bank, amount)
RepayLoan(bank, amount)
AdjustExchangeRate(new_rate)
GetEconomicStats()
```

### Events to Track
- `OnGoldDeposited`: When gold is added to reserves
- `OnCurrencyCreated`: When new money enters circulation
- `OnCurrencyDestroyed`: When money is removed
- `OnBankLoan`: When a bank borrows
- `OnPolicyChange`: When monetary policy changes
