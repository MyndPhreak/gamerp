# Money Circulation & Flow

## Overview

This document explains how money moves through the economy, from creation to circulation to removal. Understanding these flows is critical for balancing the economy.

## Money Lifecycle

### 1. Money Creation (Source)

**Only One Source: Gold Mining**

```
Gold mined from earth
→ Processed into gold bars
→ Sold to Federal Reserve
→ Federal Reserve creates currency
→ Currency enters circulation
```

**Rate of Creation:**
- Controlled by mining difficulty
- Controlled by gold spawn rates
- Controlled by number of active miners
- Controlled by gold-to-currency exchange rate

**Example:**
```
10 miners working simultaneously
Each mines 3 gold/hour
Total: 30 gold bars/hour
Exchange rate: $1,000 per bar
Money creation rate: $30,000/hour
```

## 2. Money Circulation (Flow)

### Primary Circulation Loop

```
[Miners]
   ↓ (sell gold)
[Federal Reserve] (creates currency)
   ↓ (currency issued)
[Miner's Wallet]
   ↓ (spending)
[Businesses & Services]
   ↓ (revenue)
[Business Expenses] → [Employees]
   ↓ (salaries)      ↓ (spending)
[Government] ←← ←← [Taxes]
   ↓ (budget spending)
[Public Employees & Services]
   ↓ (salaries & contracts)
[Back to Economy] (spending)
```

### Secondary Circulation Paths

**Banking Loop:**
```
[Player Deposits]
   ↓
[Bank]
   ↓ (loans)
[Borrowers]
   ↓ (spending)
[Economy]
   ↓ (repayment)
[Bank]
   ↓ (interest to depositors)
[Depositors]
```

**Investment Loop:**
```
[Investor]
   ↓ (buys business/property)
[Seller]
   ↓ (receives payment)
[Economy]
   ↓ (dividends/rent)
[Investor]
```

### Velocity of Money

**Definition:** How many times money changes hands

**High Velocity (Healthy Economy):**
- Players actively spending
- Many transactions per dollar
- Services in high demand
- Businesses thriving

**Low Velocity (Stagnant Economy):**
- Players hoarding cash
- Few transactions
- Services underutilized
- Businesses struggling

**Encouraging Velocity:**
- Property taxes (cost to hold assets)
- Inflation (money loses value over time)
- Time-limited sales events
- Reward active participation

## 3. Money Removal (Sinks)

### Primary Sinks

**Taxes (Largest Sink):**
```
Money flows from players/businesses to government
→ Government spends back into economy
→ Net: Circulation, not true removal (unless surplus saved)
```

**Death/Estate Tax (True Removal):**
```
Player dies
→ 10% of wealth removed from game
→ Money destroyed
→ Reduces total money supply
```

**Bank Loan Interest (Partial Removal):**
```
Player repays $10,000 loan with $1,000 interest
→ $10,000 returns to bank circulation
→ $1,000 profit for bank (circulates if spent)
→ If bank saves profit, acts as sink
```

**Federal Reserve Buyback (True Removal):**
```
Player sells $10,000 currency to Federal Reserve
→ Receives gold bars
→ Currency destroyed
→ Total money supply decreases
```

### Secondary Sinks

**NPC Purchases (If Not Respent):**
```
Player buys item from NPC for $5,000
→ If NPC doesn't respend, money removed
→ Alternative: NPC money returns to loot/rewards
```

**Fines & Penalties:**
```
Player fined $10,000 by police
→ Money goes to government
→ Government spends or saves
```

**Business License Fees:**
```
Player pays $10,000/month license
→ Goes to government budget
→ Spent on salaries/services
```

**Equipment Repairs/Durability:**
```
Player pays $2,000 NPC for repairs
→ Money sink if NPC doesn't circulate it
```

## Economic Balance

### Inflation (Too Much Money)

**Causes:**
- Mining rate too high
- Not enough sinks
- Low money velocity (hoarding)

**Effects:**
- Prices rise
- Currency worth less
- Difficult for new players

**Solutions:**
- Reduce mining rate
- Increase tax rates
- Add more sinks (fees, costs)
- Reduce gold-to-currency ratio
- Estate tax on death
- Property tax increases

### Deflation (Too Little Money)

**Causes:**
- Mining rate too low
- Too many sinks
- Money hoarding in banks

**Effects:**
- Prices fall
- Hard to get currency
- Economic slowdown
- Player frustration

**Solutions:**
- Increase mining rate
- Reduce tax rates
- Remove some sinks
- Increase gold-to-currency ratio
- Government stimulus (spending)
- Reduce license fees

### Target: Stable Money Supply

**Goal:** Money supply grows with server population

**Formula:**
```
Target Money Supply = (Active Players) × (Target Per-Player Wealth)

Example:
50 active players
Target: $500,000 per player
Total target: $25,000,000

Current supply: $20,000,000
Action: Increase mining rate or reduce sinks
```

## Money Flow Metrics

### Tracking Economic Health

**Key Metrics:**

**Total Money Supply:**
- All currency in circulation
- Displayed by Federal Reserve
- Tracked over time

**Money Creation Rate:**
- Currency created per hour/day
- From gold mining
- Should match economic growth

**Money Removal Rate:**
- Currency destroyed per hour/day
- From sinks (death tax, buybacks)
- Should balance creation for stability

**Money Velocity:**
- Transactions per dollar
- Higher = healthier economy

**Wealth Distribution:**
- Average player wealth
- Median player wealth
- Top 10% wealth share
- Gini coefficient (inequality)

### Admin Dashboard

**Display:**
- Total money supply (real-time)
- Money created (last hour/day/week)
- Money removed (last hour/day/week)
- Net change (growth/shrinkage)
- Average player balance
- Largest balances (top 10)
- Transaction volume
- Tax revenue

**Alerts:**
- Money supply growing >10%/day (inflation risk)
- Money supply shrinking >5%/day (deflation risk)
- Single player holds >20% of money supply (monopoly)
- Low transaction volume (stagnant economy)

## Circulation Scenarios

### Scenario 1: Healthy Economy

```
Week 1:
- Miners create $5,000,000
- Players spend $4,800,000 (96% velocity)
- Sinks remove $200,000
- Net growth: $4,800,000
- Total supply: $10,000,000

Week 2:
- Miners create $5,500,000 (more players)
- Players spend $5,300,000
- Sinks remove $300,000
- Net growth: $5,200,000
- Total supply: $15,200,000

Analysis: Healthy growth, high velocity, balanced
```

### Scenario 2: Inflation Crisis

```
Week 1:
- Miners create $10,000,000 (too much)
- Players spend $7,000,000 (70% velocity)
- Sinks remove $500,000
- Net growth: $9,500,000
- Total supply: $20,000,000

Week 2:
- Miners create $12,000,000 (more miners)
- Players spend $9,000,000
- Sinks remove $600,000
- Net growth: $11,400,000
- Total supply: $31,400,000

Effects:
- Prices doubling
- New players can't afford anything
- Currency devaluing

Action:
- Reduce gold spawn rate by 50%
- Increase estate tax to 20%
- Increase property tax
- Reduce gold-to-currency ratio to $500/bar
```

### Scenario 3: Deflation Crisis

```
Week 1:
- Miners create $1,000,000 (too little)
- Players spend $800,000
- Sinks remove $300,000
- Net growth: $700,000
- Total supply: $5,000,000

Week 2:
- Miners create $800,000 (miners leaving)
- Players spend $600,000
- Sinks remove $400,000
- Net growth: $400,000
- Total supply: $5,400,000

Effects:
- Not enough money circulating
- Can't afford services
- Businesses failing
- Players leaving

Action:
- Increase gold spawn rate by 100%
- Reduce all taxes by 30%
- Increase gold-to-currency ratio to $2,000/bar
- Government stimulus spending
```

## Player Wealth Distribution

### Natural Distribution

**Expected Distribution:**
- Bottom 25%: $10,000-50,000 (new/casual players)
- Middle 50%: $50,000-500,000 (active players)
- Top 25%: $500,000-5,000,000 (business owners, veterans)
- Top 1%: $5,000,000+ (successful tycoons)

**Preventing Extreme Inequality:**
- Progressive income tax
- Estate tax on death
- Property tax (hits wealthy harder)
- Wealth redistribution through government spending
- Opportunities for new players

**Encouraging Inequality (For Motivation):**
- Skill-based rewards
- Business success
- Smart investments
- Risk-taking pays off

### Wealth Mobility

**Upward Mobility:**
- New player → Miner → $100,000 in first week
- $100,000 → Start business → $500,000 in month
- $500,000 → Expand → $2,000,000 in 2 months

**Downward Mobility:**
- Bad investments
- Business failure
- Death and estate tax
- Fines and legal costs

## Transaction Types

### Direct Player-to-Player
```
Player A → $5,000 → Player B
(service payment, trade, gift)
```

### Player-to-Business
```
Player → $2,000 → Gun Shop
(purchase, payment for goods)
```

### Player-to-Government
```
Player → $10,000 → Government
(taxes, fines, license fees)
```

### Government-to-Player
```
Government → $1,500 → Police Officer
(salary payment)
```

### Player-to-Bank
```
Player → $50,000 → Bank
(deposit, loan repayment)
```

### Bank-to-Player
```
Bank → $100,000 → Player
(loan, withdrawal)
```

### Player-to-Federal-Reserve
```
Player → 10 gold bars → Federal Reserve
Federal Reserve → $10,000 → Player
(gold sale, currency creation)
```

### Player-to-NPC
```
Player → $3,000 → NPC Vendor
(item purchase from game system)
```

## Circulation Optimization

### Encouraging Circulation

**Mechanics:**
- Time-limited events (spend money now)
- Discounts for quick purchases
- Rewards for frequent transactions
- Penalties for hoarding

**Social:**
- Leaderboards (most transactions)
- Reputation for active traders
- Community events requiring spending

**Economic:**
- Inflation (holding cash loses value)
- Investment opportunities (stocks, bonds)
- Property appreciation (buy real estate)

### Preventing Stagnation

**Problems:**
- Rich players hoard wealth
- New players can't earn
- Services not utilized

**Solutions:**
- Property tax (forces spending or selling)
- Idle wealth tax (controversial)
- Encourage investment in new players
- Government contracts for all skill levels

## Integration with Other Systems

### Federal Reserve
- Tracks total money supply
- Controls creation rate
- Enables destruction (buyback)

### Banking
- Facilitates transactions
- Stores idle money
- Creates credit (temporary money)

### Government
- Major circulation hub
- Collects from all players
- Spends back broadly

### Taxation
- Main redistribution mechanism
- Prevents excessive accumulation
- Funds public goods

## Implementation Notes

### Real-time Tracking
```
MoneySupplyTracker {
  total_supply: integer
  created_last_hour: integer
  removed_last_hour: integer
  transaction_count: integer
  velocity: float
}
```

### Transaction Logging
```
Transaction {
  id: unique_id
  from: player_id or system
  to: player_id or system
  amount: integer
  type: enum
  timestamp: timestamp
}
```

### Economic Report Generator
```
GenerateReport(timeframe) {
  money_created
  money_removed
  net_change
  total_supply
  avg_player_balance
  median_player_balance
  transaction_volume
  velocity
  top_earners
  top_spenders
}
```

### Events
- `OnMoneyCreated`: Currency enters economy
- `OnMoneyDestroyed`: Currency removed
- `OnTransaction`: Money changes hands
- `OnInflationAlert`: Money supply growing too fast
- `OnDeflationAlert`: Money supply shrinking
- `OnVelocityChange`: Transaction rate changes
