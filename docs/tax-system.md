# Tax System

## Overview

Taxes are the primary way money circulates from the private sector to the government. All tax revenue funds government operations, salaries, and public services.

## Tax Types

### 1. Income Tax
**Who Pays:** All players earning income

**Rates:**
- Low income (<$50,000/week): 15%
- Middle income ($50,000-$200,000/week): 20%
- High income (>$200,000/week): 25%

**Collection:**
- Automatically deducted from paychecks
- Calculated on gross income
- Weekly/bi-weekly filing

**Income Sources Taxed:**
- Job salaries (government and private)
- Business profits
- Gold mining proceeds
- Investment income (dividends, interest)
- Rental income

**Example:**
```
Miner earns $100,000/week from gold sales
Tax bracket: 20%
Tax owed: $20,000
Take-home: $80,000
```

### 2. Sales Tax
**Who Pays:** Anyone purchasing goods

**Rate:** 5-8% (government configurable)

**Applied To:**
- NPC shop purchases
- Player-to-player transactions (optional)
- Service payments
- Vehicle purchases
- Weapon purchases

**Collection:**
- Automatically added at checkout
- Collected by seller
- Remitted to government weekly

**Exemptions:**
- Food items (basic necessities)
- Medical services
- Educational services
- Government services

**Example:**
```
Player buys gun for $5,000
Sales tax: 7%
Tax amount: $350
Total paid: $5,350

Seller receives: $5,000
Government receives: $350
```

### 3. Property Tax
**Who Pays:** Property and business owners

**Rate:** 1-2% of property value annually

**Calculation:**
```
Property value: $500,000
Tax rate: 2% per year
Annual tax: $10,000
Collected: $192/week (real-time)
```

**Collection:**
- Weekly/monthly installments
- Automatic deduction from bank account
- Late fees for non-payment (10% penalty)
- Property seizure after 3 missed payments

**Property Types:**
- Residential homes
- Commercial buildings
- Businesses
- Warehouses
- Land parcels

### 4. Business License Fees
**Who Pays:** Business owners

**Fees by Business Type:**
- **Basic Shop**: $5,000/month
- **Restaurant**: $7,500/month
- **Bank**: $15,000/month
- **Mining Company**: $10,000/month
- **Gun Shop**: $12,000/month
- **Vehicle Dealership**: $10,000/month

**Purpose:**
- Regulate business formation
- Generate consistent revenue
- Prevent business spam
- Fund business inspections

**Renewal:**
- Monthly automatic payment
- Business closes if not paid
- No refunds for early closure

### 5. Vehicle Registration
**Who Pays:** Vehicle owners

**Fees:**
- **Motorcycles**: $500/year
- **Cars**: $1,000/year
- **Trucks**: $1,500/year
- **Commercial Vehicles**: $2,500/year

**Collection:**
- Annual renewal
- Must register within 1 week of purchase
- Unregistered vehicles can be impounded
- Registration shows on vehicle (license plate)

### 6. Import/Export Tariffs
**Who Pays:** Businesses importing goods

**Rate:** 10% of goods value

**Applied To:**
- Items from outside the city (if applicable)
- Special items from events
- Cross-server trade (if implemented)

**Purpose:**
- Protect local businesses
- Generate revenue
- Control item flow

### 7. Capital Gains Tax
**Who Pays:** Players selling assets for profit

**Rate:** 15% of profit

**Applied To:**
- Property flipping (buy low, sell high)
- Vehicle resale
- Business sales
- Stock market gains (if implemented)

**Calculation:**
```
Bought property for: $200,000
Sold property for: $300,000
Profit: $100,000
Capital gains tax: $15,000
Net profit: $85,000
```

### 8. Estate Tax (Death Tax)
**Who Pays:** Players who die (or their estates)

**Rate:** 10% of total wealth

**Applied To:**
- Cash on hand
- Bank balances
- Property values
- Vehicle values
- Business values

**Purpose:**
- Money sink (removes money from economy)
- Creates risk in dangerous activities
- Prevents infinite wealth accumulation

**Example:**
```
Player dies with:
- $50,000 cash
- $200,000 in bank
- $500,000 property
Total wealth: $750,000

Estate tax: 10% = $75,000
Inheritable wealth: $675,000
```

## Tax Collection Mechanics

### Automatic Collection
Most taxes are collected automatically:
- Income tax: Deducted from paychecks
- Sales tax: Added at point of sale
- Property tax: Auto-withdrawal from bank
- Vehicle registration: Renewal notification + auto-pay

### Manual Filing (Business Taxes)
Business owners file weekly/monthly:
```
1. Calculate gross revenue
2. Deduct business expenses
3. Calculate net profit
4. Apply tax rate
5. Submit payment to government
6. Receive confirmation
```

**Audit Risk:**
- Random audits by government
- Check reported vs. actual income
- Penalties for tax evasion (2x owed amount + jail time)

### Payment Methods
- Direct bank withdrawal (automatic)
- Cash payment at government office
- Wire transfer to government account

### Late Payment
- **Week 1 late**: 10% penalty
- **Week 2 late**: 25% penalty + asset freezing warning
- **Week 3 late**: Asset seizure, license suspension

## Tax Revenue Usage

### Government Budget Allocation
All tax revenue goes to government treasury:

**Mandatory Spending (70%):**
- Police salaries (25%)
- EMS/Fire salaries (15%)
- Government employee salaries (10%)
- Infrastructure maintenance (10%)
- Deposit insurance fund (10%)

**Discretionary Spending (30%):**
- Public works projects
- Subsidies for key industries
- Welfare programs
- Education (if implemented)
- Defense/military (if implemented)

### Budget Transparency
- Public dashboard shows:
  - Total tax revenue collected
  - Spending by category
  - Budget surplus/deficit
  - Upcoming payments

## Tax Incentives & Deductions

### Business Deductions
Businesses can deduct:
- Employee salaries (reduce taxable profit)
- Equipment purchases
- Rent/lease payments
- Operating expenses
- Depreciation

**Example:**
```
Business Revenue: $500,000
Salaries paid: $200,000
Rent: $50,000
Supplies: $50,000
Total deductions: $300,000

Taxable profit: $200,000
Tax rate: 20%
Tax owed: $40,000 (instead of $100,000 on revenue)
```

### Tax Credits
Government can offer incentives:
- **New Business Credit**: 50% off first year taxes
- **Hiring Credit**: $1,000 per employee hired
- **Green Energy Credit**: Discounts for eco-friendly operations
- **Mining Credit**: Encourage gold production

### Tax Havens (Advanced)
- Special economic zones with lower taxes
- Attracts businesses
- Creates geographic dynamics
- Government controls zones

## Tax Evasion & Enforcement

### Illegal Tax Evasion
**Methods Players Might Try:**
- Underreporting income
- Hiding assets
- Fake business expenses
- Unregistered businesses
- Cash-only transactions (avoiding sales tax)

**Detection:**
- Random audits
- Automated inconsistency checks
- Player reports
- Admin monitoring tools

**Penalties:**
- Fines (2x owed amount)
- Jail time (scaled to offense)
- Asset seizure
- Business closure
- Criminal record (affects credit score)

### Legal Tax Avoidance
**Allowed Strategies:**
- Maximize business deductions (legitimate expenses)
- Use tax credits
- Operate in low-tax zones
- Charitable donations (tax-deductible)
- Strategic timing of income/expenses

## Special Tax Events

### Tax Holidays
Government can declare tax-free periods:
- No sales tax weekend (stimulate economy)
- No income tax for new players (first week)
- No property tax during recession

### Emergency Taxes
During crisis, government can levy:
- War tax (additional 5% income tax)
- Crisis fee (one-time payment)
- Requires majority vote (if democratic)

### Tax Reform
Government can adjust rates:
- Increase during deficit
- Decrease during surplus
- Change brackets
- Add/remove tax types

## UI/UX Elements

### Player Tax Dashboard
- Current tax bracket
- Year-to-date taxes paid
- Upcoming tax payments
- Tax filing status
- Deductions available
- Payment history

### Business Tax Interface
- File taxes (quarterly/monthly)
- Report revenue and expenses
- Calculate tax owed
- Make payment
- Download records

### Government Tax Admin
- Total revenue by tax type
- Compliance rates
- Audit queue
- Tax rate adjustment tools
- Revenue projections

## Integration with Other Systems

### Federal Reserve
- Government deposits tax revenue
- Can borrow if deficit spending
- Pays interest on loans

### Banking System
- Auto-withdrawals for taxes
- Tax payment processing
- Records for audits

### Jobs & Businesses
- Income reporting
- Payroll tax withholding
- Business expense tracking

### Law Enforcement
- Enforce tax laws
- Arrest evaders
- Seize assets

## Anti-Exploit Measures

### Prevent Tax Evasion Exploits
- Server-side income tracking
- All transactions logged
- Cross-reference income sources
- Automated red flags

### Prevent Tax Bugs
- Atomic tax calculations
- Prevent negative tax amounts
- Cap maximum tax rate
- Verify all deductions

### Fair Assessment
- Transparent tax formulas
- Public tax code
- Appeal process for disputes
- Admin oversight

## Implementation Notes

### Database Schema
```
TaxRecord {
  id: unique_id
  player_id: foreign_key
  tax_type: enum
  amount: integer
  period: date
  paid: boolean
  due_date: date
}

TaxRate {
  tax_type: enum
  rate: float
  bracket_min: integer (for income tax)
  bracket_max: integer
  effective_date: date
}

BusinessTaxFiling {
  id: unique_id
  business_id: foreign_key
  revenue: integer
  expenses: integer
  taxable_profit: integer
  tax_owed: integer
  filed_date: timestamp
}
```

### Key Functions
```
CalculateIncomeTax(player, income)
CollectSalesTax(transaction, amount)
AssessPropertyTax(property)
ProcessBusinessLicense(business)
FileBusinessTaxes(business, revenue, expenses)
AuditPlayer(player_id)
IssueTaxRefund(player, amount)
SeizeAssetForNonPayment(player, asset)
```

### Events
- `OnTaxCollected`: Tax payment received
- `OnTaxDue`: Payment deadline approaching
- `OnTaxDelinquent`: Missed payment
- `OnAudit`: Player/business audited
- `OnTaxRateChange`: Government adjusts rates
