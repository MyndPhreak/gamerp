# Economy System Documentation

Complete documentation for the gold-backed, closed-loop economy system for S&Box roleplay gamemode.

## Table of Contents

### System Design
1. **[Overview](overview.md)** - High-level system concepts and principles
2. **[Money Circulation](money-circulation.md)** - How money flows through the economy
3. **[Federal Reserve](federal-reserve.md)** - Central banking and currency issuance
4. **[Gold Mining](gold-mining.md)** - The source of all money creation
5. **[Banking System](banking-system.md)** - Player-owned banks, deposits, and loans
6. **[Tax System](tax-system.md)** - All tax types and collection mechanisms
7. **[Government & Budget](government-budget.md)** - Government finance and spending
8. **[Jobs & Services](jobs-services.md)** - Public and private sector employment

### Technical Implementation
9. **[Implementation Guide](implementation.md)** - Phase-by-phase development plan
10. **[Event Sourcing Architecture](event-sourcing-architecture.md)** - C# event-sourced design for S&Box
11. **[Anti-Exploit Mechanisms](anti-exploit.md)** - Security and fraud prevention

## Quick Start

### For Server Owners
1. Read [Overview](overview.md) to understand the system
2. Review [Implementation Guide](implementation.md) for setup steps
3. Configure economy parameters based on server size
4. Set up [Anti-Exploit Mechanisms](anti-exploit.md)
5. Launch with Phase 1 features

### For Developers
1. Read [Event Sourcing Architecture](event-sourcing-architecture.md) for code structure
2. Follow [Implementation Guide](implementation.md) phases
3. Implement aggregates, commands, and events
4. Set up event store and projections
5. Test thoroughly with [Anti-Exploit](anti-exploit.md) measures

### For Players
1. Start by reading [Gold Mining](gold-mining.md) to earn your first money
2. Learn about [Jobs & Services](jobs-services.md) for employment options
3. Understand [Tax System](tax-system.md) to manage finances
4. Use [Banking System](banking-system.md) for savings and loans

## System Overview

This economy system is designed with these core principles:

- **No Free Money**: All currency must originate from gold mining
- **Gold-Backed**: Every dollar is backed by physical gold reserves
- **Closed-Loop**: Money circulates through mining → players → taxes → government → services
- **Event-Sourced**: Complete audit trail of all economic activity
- **Realistic**: Banking, taxation, and government budgets work like real economies

## Key Features

### Federal Reserve
- Issues currency only when gold is deposited
- Maintains 100% gold backing
- Controls monetary policy
- Provides economic transparency

### Gold Mining
- ONLY source of new money
- Balanced risk/reward locations
- Rate controls prevent inflation
- Mining companies can hire employees

### Banking System
- Player-owned banks
- Deposits earn interest
- Loans create credit
- Reserve requirements enforced
- Banks can fail if mismanaged

### Tax System
- Income tax (progressive)
- Sales tax
- Property tax
- Business licenses
- Vehicle registration
- Estate tax (on death)

### Government
- Collects all taxes
- Pays public sector salaries
- Funds services and infrastructure
- Must balance budget
- Can borrow from Federal Reserve

### Jobs
- **Public**: Police, EMS, Fire, Government workers
- **Private**: Miners, Bankers, Shop Owners, Mechanics, Security

## Architecture Highlights

### Event Sourcing
The system uses event sourcing to:
- Maintain complete transaction history
- Enable time-travel debugging
- Provide audit trails
- Support multiple read models
- Ensure data integrity

### Aggregates
- **WalletAggregate**: Player money
- **FederalReserveAggregate**: Currency issuance
- **BankAggregate**: Player banks
- **GovernmentAggregate**: Tax and spending

### Projections
- Wallet balances (fast queries)
- Economic statistics
- Transaction history
- Audit reports

## Implementation Phases

### Phase 1: Foundation (Week 1-2)
- Currency system
- Federal Reserve
- Gold mining
- Basic transactions

### Phase 2: Taxation (Week 3)
- Income tax
- Sales tax
- Government treasury

### Phase 3: Jobs (Week 4)
- Public sector jobs
- Salary payments
- Clock in/out

### Phase 4: Banking (Week 5-6)
- Player banks
- Deposits/withdrawals
- Loans
- Reserve requirements

### Phase 5: Advanced Taxes (Week 7)
- Property tax
- Business licenses
- Estate tax

### Phase 6: UI/UX (Week 8)
- Dashboards
- Banking interfaces
- Financial reports

### Phase 7: Security (Week 9)
- Anti-exploit measures
- Admin tools
- Rate limiting

### Phase 8: Balancing (Week 10+)
- Economic tuning
- Performance optimization
- Beta testing

## Configuration

### Server Size Scaling

**Small Server (10-20 players):**
```csharp
GoldSpawnRate = 0.5f;
ExchangeRate = 1000f;
IncomeTaxRate = 0.15f;
```

**Medium Server (20-50 players):**
```csharp
GoldSpawnRate = 1.0f;
ExchangeRate = 1000f;
IncomeTaxRate = 0.20f;
```

**Large Server (50+ players):**
```csharp
GoldSpawnRate = 2.0f;
ExchangeRate = 1000f;
IncomeTaxRate = 0.25f;
```

## Monitoring

### Key Metrics
- Total money supply
- Money creation rate (per hour)
- Money destruction rate
- Inflation/deflation indicators
- Average player wealth
- Wealth distribution (Gini coefficient)
- Transaction volume
- Tax revenue
- Government budget balance

### Admin Dashboard
Real-time monitoring of:
- Federal Reserve status
- Government treasury
- Active banks
- Top earners/spenders
- Recent large transactions
- Economic health alerts

## Support & Resources

### Documentation
- Full design docs in this folder
- Code examples in [Event Sourcing Architecture](event-sourcing-architecture.md)
- Implementation guide with phases

### Community
- Report issues on GitHub
- Discuss on Discord
- Share server configs
- Contribute improvements

### References
- S&Box Documentation: https://docs.facepunch.com/s/sbox-dev/
- Event Sourcing patterns
- Real-world economic systems (Federal Reserve, banking)
- DarkRP for inspiration

## License

[Your License Here]

## Credits

Designed for S&Box by Facepunch Studios
Economy system architecture by [Your Name]

---

**Last Updated**: 2026-01-03
**Version**: 1.0
**Status**: Documentation Complete
