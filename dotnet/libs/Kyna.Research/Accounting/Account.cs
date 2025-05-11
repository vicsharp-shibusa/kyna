using System.Runtime.CompilerServices;

namespace Kyna.Research.Accounting;

internal class Account
{
    //public event EventHandler<AccountingEventArgs>? RaiseAccountEvent;

    public Account()
    {
        //PlayerName = playerName;
        Ledger = new Ledger();
    }

    //public string PlayerName { get; init; }

    public Guid Id { get; } = Guid.NewGuid();
    public Ledger Ledger { get; }

    public AccountSummary RecordInterestIncome(DateOnly date, decimal amount)
    {
        if (amount > 0M)
        {
            Ledger.AddEntry(date, Constants.Accounting.AccountNames.Cash, amount,
                Constants.Accounting.AccountNames.InterestIncome, amount,
                new InstrumentTransaction(InstrumentTransactionType.InterestIncome, date, "INTEREST", 0, 0),
                $"Interest Income: {amount:C}");

            //RaiseAccountEvent?.Invoke(this, new AccountingEventArgs()
            //{
            //    Date = date,
            //    InterestIncomeAmount = amount
            //});
        }

        return GetAccountSummary(date);
    }

    public AccountSummary RecordDividendIncome(DateOnly date, decimal amount)
    {
        if (amount > 0M)
        {
            Ledger.AddEntry(date, Constants.Accounting.AccountNames.Cash, amount,
                Constants.Accounting.AccountNames.DividendIncome, amount,
                new InstrumentTransaction(InstrumentTransactionType.DividendIncome, date, "DIVIDEND", 0, 0),
                $"Dividend Income: {amount:C}");

            //RaiseAccountEvent?.Invoke(this, new AccountingEventArgs()
            //{
            //    Date = date,
            //    DividendIncomeAmount = amount
            //});
        }

        return GetAccountSummary(date);
    }

    public AccountSummary DepositFunds(DateOnly date, decimal amount)
    {
        if (amount > 0M)
        {
            Ledger.AddEntry(date, Constants.Accounting.AccountNames.Cash, amount,
                Constants.Accounting.AccountNames.OwnersEquity, amount,
                description: $"Deposit: {amount:C}");
        }

        return GetAccountSummary(date);
    }

    public bool TryWithdrawFunds(DateOnly date, decimal amount, out AccountSummary accountSummary)
    {
        var absAmount = Math.Abs(amount);

        var ledger = new Ledger(Ledger.Entries);
        ledger.Withdraw(date, absAmount);

        var isValid = Ledger.IsValid(ledger, date, out var _);

        if (isValid)
        {
            Ledger.Withdraw(date, absAmount);

            //RaiseAccountEvent?.Invoke(this, new AccountingEventArgs()
            //{
            //    Date = date,
            //    WithdrawalAmount = absAmount
            //});
        }

        accountSummary = GetAccountSummary(date);

        return isValid;
    }

    public bool TryBuyInstrument(DateOnly date, string name, int quantity, decimal price,
        out AccountSummary accountSummary, out string? message)
    {
        message = null;
        decimal amount = Math.Abs(quantity * price);
        bool isValid;
        var entry = new LedgerEntry(date, name, amount, Constants.Accounting.AccountNames.Cash, amount,
            new InstrumentTransaction(InstrumentTransactionType.Buy, date, name, quantity, price),
            $"Buy {quantity} {name} at {price:C}");

        using (var ledger = new Ledger(Ledger.Entries))
        {
            ledger.AddEntry(entry);
            isValid = Ledger.IsValid(ledger, date, out message);
        }

        if (isValid)
        {
            Ledger.AddEntry(entry);
        }

        accountSummary = GetAccountSummary(date);
        return isValid;
    }

    public bool TrySellInstrument(DateOnly date, string name, int quantity, decimal price,
        out AccountSummary accountSummary, out string? message)
    {
        message = null;
        bool isValid;

        decimal amount = Math.Abs(quantity * price);

        var entry = new LedgerEntry(date, Constants.Accounting.AccountNames.Cash, amount, name, amount,
            new InstrumentTransaction(InstrumentTransactionType.Sell, date, name, quantity, price),
            $"Sell {quantity} {name} at {price:C}");

        using (var ledger = new Ledger(Ledger.Entries))
        {
            ledger.AddEntry(entry);
            isValid = Ledger.IsValid(ledger, date, out message);
        }

        if (isValid)
        {
            Ledger.AddEntry(entry);
        }

        accountSummary = GetAccountSummary(date);
        return isValid;
    }

    public AccountSummary GetAccountSummary(DateOnly? asOfDate = null)
    {
        asOfDate ??= DateOnly.FromDateTime(DateTime.Now);

        var transactions = Ledger.Entries.Where(e => e.Date <= asOfDate).ToArray();

        var deposits = transactions.Where(t => t.IsDeposit);
        var withdrawals = transactions.Where(t => t.IsWithdrawal);
        var purchases = transactions.Where(t => t.IsPurchase);
        var sales = transactions.Where(t => t.IsSale);
        var instrumentTransactions = purchases.Union(sales);
        var interestIncome = transactions.Where(t => t.IsInterestIncome).Select(t => t.TotalDebits).Sum();
        var dividendIncome = transactions.Where(t => t.IsDividendIncome).Select(t => t.TotalDebits).Sum();

        var totalDeposits = deposits.Sum(d => d.TotalDebits);
        var totalWithdrawals = withdrawals.Sum(w => w.TotalCredits);
        var totalPurchases = purchases.Sum(p => p.TotalCredits);
        var totalSales = sales.Sum(s => s.TotalDebits);
        var totalInvestment = totalDeposits - totalWithdrawals;
        var totalCash = totalInvestment + totalSales + interestIncome + dividendIncome - totalPurchases;

        var totalBookValue = Ledger.GetPositions(asOfDate)
            .GroupBy(p => p.Instrument)
            .Select(p => p.LastOrDefault().BookValue).Sum();

        var totalLiquidValue = Ledger.GetPositions(asOfDate)
            .GroupBy(p => p.Instrument)
            .Select(p => p.LastOrDefault().LiquidValue).Sum();

        var liquidValue = totalCash + totalLiquidValue;

        var earliestDate = transactions.MinBy(t => t.Date)?.Date;

        return new AccountSummary(Id, earliestDate, asOfDate,
            totalCash, instrumentTransactions.Count(),
            totalBookValue, liquidValue, totalDeposits, totalWithdrawals, interestIncome, dividendIncome);
    }
}