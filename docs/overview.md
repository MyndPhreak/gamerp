# Economy System Overview

## Concept

This is a closed-loop, gold-backed economy system for an S&Box roleplay gamemode where:
- **No free money** - All currency must originate from a source
- **Gold-backed currency** - Every dollar is backed by gold reserves
- **Federal Reserve** - Central authority that issues currency
- **Money must circulate** - Through taxes, services, and transactions

## Core Principles

### 1. Money Creation
- Gold mining is the **ONLY** way new money enters the economy
- Miners extract gold and sell it to the Federal Reserve
- Federal Reserve issues currency based on gold deposits
- Exchange rate: 1 gold bar = $1,000 (configurable)

### 2. Money Circulation
- Players earn money through jobs and businesses
- Government collects taxes
- Government spends on salaries and services
- Banks facilitate savings and loans
- Money flows continuously through the economy

### 3. Money Removal
- Taxes remove money from circulation (returns to government)
- Loan interest (returns to banks)
- Federal Reserve can buy back currency for gold (deflationary)

## Economic Actors

### Federal Reserve
- Issues currency backed by gold
- Maintains gold reserves
- Sets monetary policy
- Cannot be player-controlled

### Government
- Collects taxes
- Pays public sector salaries
- Funds public services
- Can be player-controlled (elected officials)

### Banks (Player Businesses)
- Accept deposits
- Issue loans
- Pay interest on deposits
- Charge interest on loans

### Players
- Mine gold
- Work jobs
- Own businesses
- Pay taxes
- Use services

## Key Features

- **Zero-sum economy** - Money doesn't spawn from nothing
- **Realistic banking** - Banks can fail if mismanaged
- **Government budget** - Must balance revenue and spending
- **Tax system** - Multiple tax types fund government
- **Service economy** - Players provide services to each other

## Economic Flow

```
Gold Mining → Federal Reserve → Currency Creation
                    ↓
            Players & Businesses
                    ↓
            Taxes → Government
                    ↓
            Public Services & Salaries
                    ↓
            Back to Economy
```

## Documentation Structure

- [Federal Reserve System](federal-reserve.md)
- [Gold Mining System](gold-mining.md)
- [Banking System](banking-system.md)
- [Tax System](tax-system.md)
- [Government & Budget](government-budget.md)
- [Jobs & Services](jobs-services.md)
- [Money Circulation](money-circulation.md)
- [Anti-Exploit Mechanisms](anti-exploit.md)
- [Implementation Guide](implementation.md)
