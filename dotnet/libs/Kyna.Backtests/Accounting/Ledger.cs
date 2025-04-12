using System.Diagnostics;

namespace Kyna.Backtests.Accounting;

internal class Ledger : IDisposable
{
    private readonly List<LedgerEntry> _ledgerEntries = new(500);

    public Ledger()
    {

    }

    public Ledger(IEnumerable<LedgerEntry> entries)
    {
        _ledgerEntries.AddRange(entries);
    }

    public IEnumerable<LedgerEntry> Entries => _ledgerEntries.OrderBy(e => e.Date);

    public IEnumerable<LedgerEntry> TransactionEntries => Entries.Where(e => e.IsInstrumentTransaction);

    public void AddEntry(LedgerEntry entry)
    {
        _ledgerEntries.Add(entry);
    }

    public void Deposit(DateOnly date, decimal amount)
    {
        if (amount > 0)
        {
            var entry = new LedgerEntry()
            {
                Date = date,
                Description = $"Deposit {amount:C}",
                Values = [
                    new LedgerEntryValue(LedgerEntryType.Debit, date, Constants.Accounting.AccountNames.Cash, amount),
                    new LedgerEntryValue(LedgerEntryType.Credit, date, Constants.Accounting.AccountNames.OwnersEquity, amount)
                ]
            };
            AddEntry(entry);
        }
    }

    public void Withdraw(DateOnly date, decimal amount)
    {
        if (amount > 0)
        {
            var entry = new LedgerEntry()
            {
                Date = date,
                Description = $"Withdraw {amount:C}",
                Values = [
                    new LedgerEntryValue(LedgerEntryType.Debit, date, Constants.Accounting.AccountNames.OwnersEquity, amount),
                    new LedgerEntryValue(LedgerEntryType.Credit, date, Constants.Accounting.AccountNames.Cash, amount)
                ]
            };
            AddEntry(entry);
        }
    }

    protected internal static bool IsValid(Ledger ledger, DateOnly date, out string? message)
    {
        decimal cashBalance = 0M;
        message = null;

        foreach (var entry in ledger.Entries)
        {
            if (entry.Date > date)
            {
                break;
            }

            if (entry.IsWithdrawal)
            {
                cashBalance -= entry.Credits.Sum(c => c.Value);
            }

            if (entry.IsDeposit)
            {
                cashBalance += entry.Debits.Sum(d => d.Value);
            }

            if (entry.IsPurchase)
            {
                cashBalance -= entry.Credits.Sum(d => d.Value);
            }

            if (entry.IsSale)
            {
                cashBalance += entry.Debits.Sum(d => d.Value);
            }

            if (cashBalance < 0)
            {
                message = $"Cash balance dropped below zero on {entry.Date}";
                return false;
            }
        }

        return true;
    }

    public void AddEntry(DateOnly date, string debitAccount, decimal debitAmount,
        string creditAccount, decimal creditAmount,
        InstrumentTransaction? instrumentTransaction = null,
        string? description = null)
    {
        if (debitAmount != creditAmount)
        {
            throw new ArgumentException("Debit and Credit amounts must match.");
        }

        var entry = new LedgerEntry
        {
            Date = date,
            Description = description,
            Values =
            [
                new LedgerEntryValue(LedgerEntryType.Debit, date, debitAccount, debitAmount),
                new LedgerEntryValue(LedgerEntryType.Credit, date, creditAccount, creditAmount),
            ],
            Transaction = instrumentTransaction
        };
        Debug.Assert(entry.IsBalanced);

        AddEntry(entry);
    }

    public IEnumerable<Position> GetPositions(DateOnly? asOfDate = null)
    {
        asOfDate ??= DateOnly.FromDateTime(DateTime.Now);

        var transactions = TransactionEntries.Where(t => t.Date <= asOfDate).ToArray();

        var uniqueAccounts = transactions.SelectMany(t => t.AccountNames)
            .Except(Constants.Accounting.AccountNames.DefaultAccountNames).Distinct().ToArray();

        foreach (var acct in uniqueAccounts)
        {
            int qty = 0;
            decimal totalValue = 0M;

            foreach (var acctTransaction in transactions.Where(t => t.AccountNames.Contains(acct))
                .OrderBy(t => t.Date))
            {
                decimal pricePerShare = 0M;
                var prevQty = qty;
                qty += acctTransaction.Transaction.GetValueOrDefault().Quantity;

                if ((prevQty < 0 && qty > 0) || (prevQty > 0 && qty < 0))
                {
                    totalValue = acctTransaction.Transaction.GetValueOrDefault().Price * qty;
                }
                else
                {
                    totalValue += acctTransaction.Transaction.GetValueOrDefault().Value;
                }

                pricePerShare = Math.Abs(qty == 0 ? 0M : totalValue / qty);

                yield return new Position()
                {
                    Date = acctTransaction.Date,
                    EntryPrice = pricePerShare,
                    Instrument = acctTransaction.Transaction.GetValueOrDefault().Instrument,
                    InstrumentMostRecentClose = null,
                    Quantity = qty
                };
            }
        }
    }

    void IDisposable.Dispose()
    {
        _ledgerEntries.Clear();
    }
}
