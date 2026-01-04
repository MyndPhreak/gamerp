# Banking System

## Overview

Banks are player-owned businesses that accept deposits, pay interest, and issue loans. They serve as financial intermediaries and create economic opportunity through credit.

## Bank Types

### Player-Owned Banks
- Created and managed by players
- Compete for customers
- Can fail if mismanaged
- Profit from interest rate spread

### NPC Banks (Optional)
- Backup system if no player banks exist
- Standard rates (lower risk, lower return)
- Always available
- Prevents banking desert

## Core Banking Functions

### 1. Deposits
**Process:**
```
Player deposits money into bank account
→ Bank records deposit in ledger
→ Bank can use deposited money for loans
→ Player earns interest on deposit
→ Player can withdraw anytime (subject to limits)
```

**Interest on Deposits:**
- Typical rate: 1-3% per real-time day/week
- Banks compete on interest rates
- Higher rates attract more deposits
- Paid automatically on interval

**Deposit Types:**
- **Checking Account**: 0.5% interest, unlimited withdrawals
- **Savings Account**: 2% interest, limited withdrawals
- **Certificate of Deposit**: 5% interest, locked for time period

### 2. Loans
**Process:**
```
Player requests loan from bank
→ Bank reviews credit score/history
→ Bank approves or denies
→ Money deposited to player account
→ Player pays monthly/weekly installments
→ Includes principal + interest
→ Collateral seized if defaulted
```

**Loan Types:**

**Personal Loan**
- Amount: $1,000 - $50,000
- Interest: 8-12%
- Duration: 1-4 weeks (real-time)
- Purpose: General use
- Collateral: Optional

**Business Loan**
- Amount: $10,000 - $500,000
- Interest: 5-10%
- Duration: 2-8 weeks
- Purpose: Start/expand business
- Collateral: Required (property, assets)

**Vehicle Loan**
- Amount: Based on vehicle value
- Interest: 6-8%
- Duration: 2-6 weeks
- Collateral: The vehicle itself
- Repossessed if defaulted

**Property Loan (Mortgage)**
- Amount: Based on property value (up to 80%)
- Interest: 4-7%
- Duration: 4-12 weeks
- Collateral: The property
- Foreclosed if defaulted

### 3. Wire Transfers
- Send money to other players instantly
- Small fee (e.g., $10-50)
- Revenue for banks
- Convenient for large transactions

### 4. ATM Network
- Remote access to bank accounts
- Check balance
- Withdraw cash (daily limit)
- Deposit cash
- Small convenience fee

## Banking Mechanics

### Reserve Requirement
**Purpose:** Ensures banks can meet withdrawal demands

**Mechanics:**
- Federal Reserve mandates reserve ratio (e.g., 10%)
- If bank has $100,000 in deposits, must keep $10,000 in reserve
- Can loan out remaining $90,000
- Banks failing to meet requirement must borrow from Federal Reserve

**Example:**
```
Total Deposits: $100,000
Reserve Requirement: 10%
Required Reserves: $10,000
Available for Loans: $90,000

If loans exceed $90,000:
→ Bank is over-leveraged
→ Must borrow from Federal Reserve
→ Pays interest to Federal Reserve
→ Reduces profit margin
```

### Interest Rate Spread
**How Banks Profit:**
```
Deposit Interest Rate: 2%
Loan Interest Rate: 8%
Spread: 6%

Example:
- Bank has $100,000 in deposits
- Pays $2,000/year in interest to depositors
- Loans out $90,000 at 8%
- Earns $7,200/year in loan interest
- Net profit: $5,200/year
- Minus operating costs
```

### Credit Score System
**Tracks Player Creditworthiness:**

**Factors:**
- Loan repayment history (70%)
- Current debt level (20%)
- Account age (5%)
- Income level (5%)

**Score Ranges:**
- 750-850: Excellent (best rates, high limits)
- 650-749: Good (standard rates)
- 550-649: Fair (higher rates, lower limits)
- Below 550: Poor (loan denial, very high rates)

**Building Credit:**
- Take small loans and repay on time
- Maintain active bank account
- Keep debt-to-income ratio low

### Bank Failures
**When Banks Fail:**
- Cannot meet withdrawal demands
- Reserves below requirement for extended period
- Too many loan defaults
- Owner mismanagement

**Failure Process:**
```
Bank becomes insolvent
→ Federal Reserve steps in
→ Deposit insurance pays depositors (up to limit)
→ Bank assets liquidated
→ Loans sold to other banks
→ Bank owner loses investment
```

**Deposit Insurance:**
- Government-backed
- Covers up to $50,000 per account
- Funded by bank insurance premiums
- Prevents panic withdrawals

## Starting a Bank

### Requirements
- **Business License**: $10,000
- **Initial Capital**: $100,000 minimum
- **Reserve Deposit**: $50,000 to Federal Reserve
- **Physical Location**: Rent or own building
- **Insurance Premium**: $5,000/month

### Setup Process
```
1. Player accumulates required capital
2. Apply for banking license (government approval)
3. Rent/buy building for bank
4. Deposit reserves with Federal Reserve
5. Set up banking systems (accounts, ATMs)
6. Open for business
7. Market to attract customers
```

### Operating Costs
- **Rent**: $2,000-5,000/week (location dependent)
- **Insurance**: $5,000/month
- **Employee Salaries**: $500/hour per teller (if hired)
- **ATM Maintenance**: $500/week per ATM
- **Marketing**: Variable

### Revenue Streams
- Loan interest (primary)
- Account fees (monthly maintenance)
- Wire transfer fees
- ATM fees (for non-customers)
- Safe deposit box rentals
- Currency exchange fees

## Bank Competition

### Competitive Factors
- **Interest Rates**: Higher deposit rates attract customers
- **Loan Availability**: Easier approval process
- **Customer Service**: Player interaction, trust
- **Locations**: More ATMs, branches
- **Reputation**: History of stability

### Market Dynamics
- Multiple banks create competition
- Players choose banks based on rates and service
- Banks can merge or be acquired
- Market share affects profitability

## Risk Management

### For Bank Owners
**Loan Defaults:**
- Screen borrowers (check credit score)
- Require collateral for large loans
- Diversify loan portfolio
- Set aside reserves for bad debt

**Bank Runs:**
- Maintain adequate reserves
- Build customer trust
- Limit withdrawal amounts during crisis
- Emergency borrowing from Federal Reserve

**Competition:**
- Competitive interest rates
- Quality customer service
- Multiple revenue streams
- Marketing and reputation

### For Players
**Choosing a Bank:**
- Check bank's financial health (publicly displayed)
- Compare interest rates
- Read reviews/reputation
- Ensure deposit insurance coverage

**Protecting Deposits:**
- Don't exceed insurance limit at one bank
- Diversify across multiple banks
- Monitor bank's reserve ratio
- Withdraw if bank seems unstable

## UI/UX Elements

### Bank Interface
**Customer View:**
- Account balance
- Transaction history
- Interest earned
- Available credit
- Loan balances and payments
- Wire transfer function

**Bank Owner View:**
- Total deposits
- Total loans outstanding
- Reserve ratio
- Profit/loss statement
- Customer list
- Loan applications
- Risk dashboard

### ATM Interface
- Quick balance check
- Withdraw (daily limit)
- Deposit
- Transfer between accounts
- Mini-statement

## Integration with Other Systems

### Federal Reserve
- Borrow when reserves are low
- Pay interest on borrowed funds
- Subject to reserve requirements
- Regulatory oversight

### Government
- Requires business license
- Pays business taxes
- Deposit insurance program
- Can be regulated/audited

### Players
- Provide banking services
- Enable large purchases
- Facilitate business growth
- Earn trust and reputation

### Economy
- Credit creation drives economic activity
- Interest rates affect borrowing
- Bank failures impact confidence
- Essential infrastructure

## Anti-Exploit Measures

### Prevent Money Laundering
- Transaction limits
- Suspicious activity monitoring
- Admin review of large transfers
- Account freezing capability

### Prevent Bank Fraud
- Owner cannot withdraw depositor funds directly
- Reserve requirements enforced automatically
- Audit trails for all transactions
- Penalties for violations

### Prevent Exploits
- Deposit/withdrawal rate limits
- Loan approval cooldowns
- Interest calculated server-side
- Atomic transactions (no partial failures)

## Implementation Notes

### Database Schema
```
Bank {
  id: unique_id
  owner_id: player_id
  name: string
  total_deposits: integer
  total_loans: integer
  reserves: integer
  deposit_interest_rate: float
  loan_interest_rate: float
  status: active|insolvent
}

BankAccount {
  id: unique_id
  bank_id: foreign_key
  player_id: foreign_key
  balance: integer
  account_type: checking|savings|cd
  interest_rate: float
  created_at: timestamp
}

Loan {
  id: unique_id
  bank_id: foreign_key
  borrower_id: player_id
  amount: integer
  interest_rate: float
  term_weeks: integer
  collateral: string
  status: active|defaulted|paid
  next_payment_due: timestamp
}
```

### Key Functions
```
CreateBank(owner, initial_capital)
OpenAccount(player, bank, type)
Deposit(account, amount)
Withdraw(account, amount)
RequestLoan(player, bank, amount, purpose)
ApproveLoan(bank, loan_id)
MakePayment(loan_id, amount)
CalculateInterest(account)
CheckReserveRequirement(bank)
ProcessBankFailure(bank)
```

### Events
- `OnBankCreated`: New bank opened
- `OnDeposit`: Money deposited
- `OnWithdrawal`: Money withdrawn
- `OnLoanIssued`: New loan created
- `OnLoanPayment`: Payment made
- `OnLoanDefault`: Loan not repaid
- `OnBankFailure`: Bank becomes insolvent
