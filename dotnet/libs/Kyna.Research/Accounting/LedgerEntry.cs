using Kyna.Common;

namespace Kyna.Research.Accounting;

internal class LedgerEntry : IEquatable<LedgerEntry?>
{
    private LedgerEntryValue[] _entryValues = [];
    private InstrumentTransaction? _transaction;

    public LedgerEntry() { }

    public LedgerEntry(DateOnly date, IEnumerable<LedgerEntryValue> entries, string? description = null)
    {
        Date = date;
        Values = [.. entries];
        Description = description;
    }

    public LedgerEntry(DateOnly date, string debitAccount, decimal debitAmount,
        string creditAccount, decimal creditAmount,
        InstrumentTransaction? instrumentTransaction = null,
        string? description = null)
    {
        if (debitAmount != creditAmount)
        {
            throw new ArgumentException("Debit and Credit amounts must match.");
        }

        Date = date;
        Description = description;
        Values = [
            new LedgerEntryValue(LedgerEntryType.Debit, date, debitAccount, debitAmount),
            new LedgerEntryValue(LedgerEntryType.Credit, date, creditAccount, creditAmount),
        ];
        Transaction = instrumentTransaction;
    }

    public DateOnly Date { get; init; }

    public string? Description { get; init; }

    public LedgerEntryValue[] Values
    {
        get => [.. _entryValues.OrderBy(v => v.Date)];
        init => _entryValues = value;
    }

    public LedgerEntryValue Debit
    {
        get
        {
            if (Debits.Length == 1)
            {
                return Debits[0];
            }
            throw new Exception("More than 1 debit available; use Debits instead.");
        }
    }

    public LedgerEntryValue Credit
    {
        get
        {
            if (Credits.Length == 1)
            {
                return Credits[0];
            }
            throw new Exception("More than 1 credit available; use Credits instead.");
        }
    }

    public LedgerEntryValue[] Debits =>
        [.. Values.Where(v => v.Type.Equals(LedgerEntryType.Debit))];

    public LedgerEntryValue[] Credits =>
        [.. Values.Where(v => v.Type.Equals(LedgerEntryType.Credit))];

    public decimal TotalDebits => Debits.Sum(d => d.Value);

    public decimal TotalCredits => Credits.Sum(c => c.Value);

    public bool IsDepositOrWithdrawal =>
        !Values.Select(v => v.AccountName).Distinct().Except(
            Constants.Accounting.AccountNames.DepositAndWithdrawalAccounts).Any() &&
        (Transaction?.Type ?? InstrumentTransactionType.None) != InstrumentTransactionType.InterestIncome;

    public bool IsDeposit => IsDepositOrWithdrawal &&
        Debit.AccountName.Equals(Constants.Accounting.AccountNames.Cash);

    public bool IsWithdrawal => IsDepositOrWithdrawal &&
        Credit.AccountName.Equals(Constants.Accounting.AccountNames.Cash);

    public bool IsInterestIncome => (Transaction?.Type ?? InstrumentTransactionType.None) ==
        InstrumentTransactionType.InterestIncome;

    public bool IsDividendIncome => (Transaction?.Type ?? InstrumentTransactionType.None) ==
        InstrumentTransactionType.DividendIncome;

    public bool IsInstrumentTransaction => !IsDepositOrWithdrawal;

    public bool IsPurchase => !IsDepositOrWithdrawal &&
        Transaction.HasValue &&
        Transaction.Value.Type.Equals(InstrumentTransactionType.Buy);

    public bool IsSale => !IsDepositOrWithdrawal &&
        Transaction.HasValue &&
        Transaction.Value.Type.Equals(InstrumentTransactionType.Sell);

    public InstrumentTransaction? Transaction
    {
        get => _transaction;
        init
        {
            _transaction = value;
            if (_transaction.HasValue &&
                _transaction.Value.Type != InstrumentTransactionType.None &&
                _entryValues.Length == 0)
            {
                _entryValues = new LedgerEntryValue[2];
                if (_transaction.Value.Type == InstrumentTransactionType.Buy)
                {
                    _entryValues[0] = new LedgerEntryValue(LedgerEntryType.Debit, _transaction.Value.Date,
                        _transaction.Value.Instrument, Math.Abs(_transaction.Value.Value), _transaction);
                    _entryValues[1] = new LedgerEntryValue(LedgerEntryType.Credit, _transaction.Value.Date,
                        Constants.Accounting.AccountNames.Cash, Math.Abs(_transaction.Value.Value), _transaction);
                }
                if (_transaction.Value.Type == InstrumentTransactionType.Sell)
                {
                    _entryValues[0] = new LedgerEntryValue(LedgerEntryType.Credit, _transaction.Value.Date,
                        _transaction.Value.Instrument, Math.Abs(_transaction.Value.Value), _transaction);
                    _entryValues[1] = new LedgerEntryValue(LedgerEntryType.Debit, _transaction.Value.Date,
                        Constants.Accounting.AccountNames.Cash, Math.Abs(_transaction.Value.Value), _transaction);
                }
            }
        }
    }

    public string[] AccountNames => [.. Values.Select(v => v.AccountName).Distinct()];

    public bool IsBalanced => Values.Where(v => v.Type.Equals(LedgerEntryType.Credit)).Select(s => s.Value).Sum() ==
        Values.Where(v => v.Type.Equals(LedgerEntryType.Debit)).Select(s => s.Value).Sum();

    public override bool Equals(object? obj) => Equals(obj as LedgerEntry);

    public bool Equals(LedgerEntry? other)
    {
        return other is not null &&
               Date.Equals(other.Date) &&
               Values.SequenceEqual(other.Values) &&
               EqualityComparer<InstrumentTransaction?>.Default.Equals(Transaction, other.Transaction);
    }

    public override int GetHashCode() => HashCode.Combine(Date, Values, Transaction);

    public static bool operator ==(LedgerEntry? left, LedgerEntry? right) => EqualityComparer<LedgerEntry>.Default.Equals(left, right);

    public static bool operator !=(LedgerEntry? left, LedgerEntry? right) => !(left == right);

    public override string ToString() => $"{Date:yyyy-MM-dd} {Description}".Trim();
}

internal record class LedgerEntryValue(LedgerEntryType Type, DateOnly Date, string AccountName, decimal Value,
    InstrumentTransaction? Transaction = null)
{
    public LedgerEntryValue? Deposit =>
        Type == LedgerEntryType.Debit && AccountName == Constants.Accounting.AccountNames.Cash ? this : null;
    public LedgerEntryValue? Withdrawal =>
        Type == LedgerEntryType.Credit && AccountName == Constants.Accounting.AccountNames.Cash ? this : null;
    public LedgerEntryValue? Purchase =>
        Type == LedgerEntryType.Debit && !Constants.Accounting.AccountNames.DefaultAccountNames.Contains(AccountName)
        ? this : null;
    public LedgerEntryValue? Sale =>
        Type == LedgerEntryType.Credit && !Constants.Accounting.AccountNames.DefaultAccountNames.Contains(AccountName)
        ? this : null;
}

public readonly struct InstrumentTransaction(InstrumentTransactionType type, DateOnly date,
    string instrument, int quantity, decimal price)
{
    public InstrumentTransactionType Type { get; } = type;
    public DateOnly Date { get; } = date;
    public string Instrument { get; } = instrument;
    public int Quantity { get; } = type switch
    {
        InstrumentTransactionType.Buy => Math.Abs(quantity),
        InstrumentTransactionType.Sell => -1 * Math.Abs(quantity),
        InstrumentTransactionType.InterestIncome => 0,
        InstrumentTransactionType.DividendIncome => 0,
        _ => throw new ArgumentException($"Invalid transaction type: {type.GetEnumDescription()}")
    };
    public decimal Price { get; } = price;
    public decimal Value => Price * Quantity;
}