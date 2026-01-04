# Gold Mining System

## Overview

Gold mining is the **ONLY** way new money enters the economy. Miners extract gold from designated locations and sell it to the Federal Reserve for currency.

## Purpose

- Controls the rate of money supply growth
- Creates a job that directly generates wealth
- Provides entry point for new players
- Balances economic growth with server population

## Mining Mechanics

### Mining Locations
- Designated mining zones on the map
- Multiple mine entrances/areas
- Visual indicators (mine shafts, caves, quarries)
- Some locations more dangerous but higher yield

### Mining Process
```
1. Player takes Mining job
2. Obtain mining equipment (pickaxe, drill, etc.)
3. Travel to mining location
4. Mine gold ore from nodes/veins
5. Collect gold ore in inventory
6. (Optional) Smelt ore into gold bars
7. Sell gold bars to Federal Reserve
8. Receive currency
```

### Mining Equipment
**Basic Tier:**
- Pickaxe (slow, cheap)
- Mining speed: 1 ore per 30 seconds
- Cost: $500 or starter equipment

**Advanced Tier:**
- Drill (faster, expensive)
- Mining speed: 1 ore per 15 seconds
- Cost: $5,000
- Requires fuel/power

**Professional Tier:**
- Industrial drill (fastest, very expensive)
- Mining speed: 1 ore per 5 seconds
- Cost: $20,000
- Requires fuel, maintenance

### Gold Ore Processing
**Option 1: Direct Sell (Quick)**
- Sell gold ore to NPC smelter
- Smelter processes and sells to Federal Reserve
- Player gets 80% of value (20% processing fee)

**Option 2: Self-Smelt (Efficient)**
- Player owns or rents smelting equipment
- Smelt ore into gold bars
- Sell bars directly to Federal Reserve
- Player gets 100% of value
- Requires initial investment

### Gold Yields
**Balance Example:**
- 1 gold ore = 1 gold bar (after smelting)
- 1 gold bar = $1,000 at Federal Reserve
- Average mining time: 20 seconds per ore
- Hourly rate: ~180 ore = $180,000/hour (theoretical max)
- Practical rate: ~$60,000-90,000/hour (travel, processing, breaks)

## Mining Locations

### Surface Deposits (Easy)
- Low security risk
- Lower yield (0.8x multiplier)
- Close to town
- Good for beginners

### Standard Mines (Medium)
- Moderate security risk
- Standard yield (1.0x multiplier)
- Requires travel
- Most common

### Deep Mines (Hard)
- High security risk (PvP enabled, NPCs, hazards)
- High yield (1.5x multiplier)
- Far from town
- Requires experience and equipment

### Underwater/Special (Expert)
- Extreme conditions
- Very high yield (2.0x multiplier)
- Specialized equipment required
- Limited access

## Risk vs. Reward

### Dangers in Mining
- **PvP**: Other players can attack and steal gold
- **NPCs**: Hostile creatures in deep mines
- **Environmental**: Cave-ins, gas, flooding
- **Equipment failure**: Tools break, need repairs

### Protection
- Mine in groups (split profits)
- Hire security players
- Use less valuable but safer locations
- Insurance for gold loss (optional)

## Economic Balancing

### Server Population Scaling
```
Small server (10-20 players):
- Fewer mining nodes
- Lower respawn rate
- Smaller gold yields

Large server (50+ players):
- More mining nodes
- Faster respawn rate
- Standard gold yields
```

### Controlling Money Supply
**Too much inflation (too much money):**
- Decrease gold ore spawn rate
- Increase mining difficulty
- Lower gold-to-currency ratio at Federal Reserve
- Add gold ore respawn cooldowns

**Too little money (deflation):**
- Increase gold ore spawn rate
- Decrease mining difficulty
- Raise gold-to-currency ratio
- Add bonus yield events

### Gold Sinks (Remove Gold from Economy)
- Government purchases gold for reserves
- Gold used in crafting (jewelry, electronics)
- Gold required for special licenses
- Gold as collateral for large loans

## Mining as a Business

### Solo Mining
- Keep all profits
- Higher risk
- Limited by personal time

### Mining Company
- Players create mining business
- Hire miners as employees
- Pay salaries (e.g., $500/hour)
- Company sells gold and keeps profit
- Scales better

### Mining Contracts
- Government contracts for gold
- Guaranteed purchase at premium ($1,100 per bar)
- Must deliver quota
- Steady income for miners

## Licensing & Regulation

### Mining License
- Required to mine legally
- Cost: $1,000 (one-time or annual)
- Revenue goes to government
- Unlicensed mining is illegal (police can fine/arrest)

### Land Claims
- Government auctions mining zones
- Highest bidder gets exclusive access
- Duration: 1 week real-time
- Creates territory control dynamics

### Environmental Regulations
- Mining tax (10% of gold sold)
- Environmental restoration fees
- Encourages efficient mining

## Anti-Exploit Measures

### Rate Limiting
- Maximum gold ore per player per hour
- Cooldowns on node respawns
- Diminishing returns (mining fatigue)

### Anti-AFK Mining
- Require active input (not just holding key)
- Random events requiring attention
- Kick inactive miners

### Anti-Duplication
- Server-side tracking of all gold
- Unique IDs for gold bars
- Transaction logging

### Fair Distribution
- Mining nodes spread across map
- No one location dominates
- Randomized spawn locations

## Starter Player Experience

### New Player Path
```
1. Join server, spawn with no money
2. Take Mining job (free)
3. Receive basic pickaxe (free starter kit)
4. Tutorial shows nearest safe mine
5. Mine 5-10 gold ore (15-30 minutes)
6. Sell to Federal Reserve
7. Receive first $5,000-10,000
8. Can now buy food, rent, start other jobs
```

### Alternative: Starter Loan
- Federal Reserve gives $5,000 loan
- Must be repaid with 10% interest ($5,500)
- Gives players options besides mining
- Teaches banking system

## UI/UX Elements

### Mining HUD
- Current gold ore in inventory
- Tool durability
- Mining speed/efficiency
- Nearby node indicators
- Danger level indicator

### Federal Reserve Exchange
- Current gold value
- Transaction history
- Total earnings from mining
- Leaderboard (optional)

## Integration with Other Systems

### With Federal Reserve
- Primary customer for gold
- Sets gold value
- Controls money supply

### With Government
- Mining licenses generate tax revenue
- Environmental regulations
- Can nationalize mines (special events)

### With Banks
- Miners deposit earnings
- Loans for equipment purchases
- Mining company business loans

### With Jobs
- Mining is a dedicated job
- Can multiclass (mine + other job)
- Mining skill progression (optional)

## Implementation Notes

### Key Variables
```
mining_speed: float (seconds per ore)
gold_ore_respawn_time: float (seconds)
tool_durability: integer (uses before break)
max_gold_per_hour: integer (anti-exploit)
danger_zone_multiplier: float (yield boost)
```

### Database Schema
```
GoldOre {
  id: unique_id
  location: vector3
  amount: integer
  last_mined: timestamp
  respawn_time: float
}

MinedGold {
  player_id: string
  amount: integer
  timestamp: timestamp
  sold_to_federal_reserve: boolean
}
```

### Events
- `OnGoldOreMined`: Player extracts ore
- `OnGoldBarCreated`: Ore smelted to bar
- `OnGoldSoldToFedReserve`: Gold exchanged for currency
- `OnMiningNodeRespawn`: New ore available
