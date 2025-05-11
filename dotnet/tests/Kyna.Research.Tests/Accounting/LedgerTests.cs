using Kyna.Common;
using Kyna.Research.Accounting;

namespace Kyna.Research.Tests.Accounting;

public class LedgerTests
{
    [Fact]
    public void DefaultAccountNames_Contains_Cash_and_OwnersEquity()
    {
        Assert.NotEmpty(Constants.Accounting.AccountNames.DefaultAccountNames);
        Assert.Contains(Constants.Accounting.AccountNames.Cash, Constants.Accounting.AccountNames.DefaultAccountNames);
        Assert.Contains(Constants.Accounting.AccountNames.OwnersEquity, Constants.Accounting.AccountNames.DefaultAccountNames);
    }

    [Fact]
    public void Ledger_AddEntry_Adds()
    {
        var sut = new Ledger();
        var entry = new LedgerEntry()
        {
            Date = new DateOnly(2000, 1, 1).FindFirstWeekday(),
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy, new DateOnly(2000, 1, 1),
                "TST", 10, 100M)
        };
        sut.AddEntry(entry);
        Assert.NotEmpty(sut.Entries);
        Assert.Equal(entry, sut.Entries.ElementAt(0));
    }

    [Fact]
    public void Ledger_AddEntry_AddsToPositions()
    {
        var sut = new Ledger();

        var trans = new InstrumentTransaction(InstrumentTransactionType.Buy,
            new DateOnly(2000, 1, 1).FindFirstWeekday(),
            "TST", 10, 100M);

        sut.AddEntry(new DateOnly(2000, 1, 4), trans.Instrument, trans.Value,
            Constants.Accounting.AccountNames.Cash, trans.Value, trans);

        var entry = sut.Entries.Last();
        var positions = sut.GetPositions(entry.Date.AddDays(1));
        Assert.NotEmpty(positions);
    }

    [Fact]
    public void Ledger_Entries_SortedByDate()
    {
        var sut = new Ledger();

        var date = new DateOnly(2000, 1, 1).FindFirstWeekday();

        var oldest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy,
            new DateOnly(2000, 1, 1).FindFirstWeekday(),
                "TST", 10, 100M)
        };

        date = new DateOnly(2020, 1, 1).FindFirstWeekday();

        var newest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Sell, new DateOnly(2020, 1, 1),
                "TST", 20, 200M)
        };

        date = new DateOnly(2010, 1, 1).FindFirstWeekday();

        var middle = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy, new DateOnly(2010, 1, 1),
                "TST", 10, 100M)
        };

        sut.AddEntry(middle);
        sut.AddEntry(newest);
        sut.AddEntry(oldest);
        Assert.NotEmpty(sut.Entries);
        Assert.Equal(oldest, sut.Entries.ElementAt(0));
        Assert.Equal(middle, sut.Entries.ElementAt(1));
        Assert.Equal(newest, sut.Entries.ElementAt(2));
    }

    [Fact]
    public void GetPositionsForDateRange_LimitsToDates()
    {
        var sut = new Ledger();
        var date = new DateOnly(2000, 1, 1).FindFirstWeekday();
        var oldest = new LedgerEntry()
        {
            Date = new DateOnly(2000, 1, 3),
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy,
               date, "TST", 10, 100M)
        };

        date = new DateOnly(2020, 1, 1).FindFirstWeekday();
        var newest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Sell,
            new DateOnly(2020, 1, 1),
                "TST", 20, 200M)
        };

        date = new DateOnly(2010, 1, 1).FindFirstWeekday();
        var middle = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy, new DateOnly(2010, 1, 1),
                "TST", 10, 100M)
        };

        sut.AddEntry(middle);
        sut.AddEntry(newest);
        sut.AddEntry(oldest);

        var positions = sut.GetPositions(middle.Date.AddDays(1)).ToArray();
        Assert.NotEmpty(positions);
        Assert.Equal(2, positions.Length);
    }

    [Fact]
    public void GetPositionsForDateRange_PositionReflectsPreDateRangeValues()
    {
        var sut = new Ledger();
        var date = new DateOnly(2000, 1, 1).FindFirstWeekday();

        var oldest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy, new DateOnly(2000, 1, 3),
                "TST", 10, 100M)
        };

        date = new DateOnly(2020, 1, 1).FindFirstWeekday();

        var newest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Sell, new DateOnly(2020, 1, 1),
                "TST", 20, 200M)
        };

        date = new DateOnly(2010, 1, 1).FindFirstWeekday();

        var middle = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy,
                new DateOnly(2010, 1, 1), "TST", 10, 100M)
        };

        sut.AddEntry(middle);
        sut.AddEntry(newest);
        sut.AddEntry(oldest);

        var positions = sut.GetPositions(middle.Date.AddDays(1)).ToArray();
        Assert.NotEmpty(positions);
        var lastPos = positions.Last();
        Assert.Equal(20, lastPos.Quantity);
        Assert.Equal(100M, lastPos.EntryPrice);
    }

    [Fact]
    public void GetPositionsForDateRange_PositionQuantityGoesToZero()
    {
        var sut = new Ledger();

        var date = new DateOnly(2000, 1, 1).FindFirstWeekday();
        var oldest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy, new DateOnly(2000, 1, 3),
                "TST", 10, 100M)
        };

        date = new DateOnly(2020, 1, 1).FindFirstWeekday();
        var newest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Sell, new DateOnly(2020, 1, 1),
                "TST", -20, 200M)
        };

        date = new DateOnly(2010, 1, 1).FindFirstWeekday();

        var middle = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy, new DateOnly(2010, 1, 1),
                "TST", 10, 100M)
        };

        sut.AddEntry(middle);
        sut.AddEntry(newest);
        sut.AddEntry(oldest);

        var positions = sut.GetPositions(newest.Date).ToArray();
        Assert.NotEmpty(positions);
        var lastPos = positions.Last();
        Assert.Equal(0, lastPos.Quantity);
        Assert.Equal(0, lastPos.EntryPrice);
        Assert.Equal(newest.Date, lastPos.Date);
    }

    [Fact]
    public void GetPositionsForDateRange_PositionQuantityGoesNegative()
    {
        var sut = new Ledger();

        var date = new DateOnly(2000, 1, 1).FindFirstWeekday();
        var oldest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy, new DateOnly(2000, 1, 3),
                "TST", 10, 100M)
        };

        date = new DateOnly(2020, 1, 1).FindFirstWeekday();
        var newest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Sell, new DateOnly(2020, 1, 1),
                "TST", -30, 200M)
        };

        date = new DateOnly(2010, 1, 1).FindFirstWeekday();
        var middle = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy, new DateOnly(2010, 1, 1),
                "TST", 10, 100M)
        };

        sut.AddEntry(middle);
        sut.AddEntry(newest);
        sut.AddEntry(oldest);

        var positions = sut.GetPositions(newest.Date).ToArray();
        Assert.NotEmpty(positions);
        var lastPos = positions.Last();
        Assert.Equal(-10, lastPos.Quantity);
        Assert.Equal(200M, lastPos.EntryPrice);
        Assert.Equal(newest.Date, lastPos.Date);
    }

    [Fact]
    public void GetPositionsForDateRange_PositionQuantityGoesPositive()
    {
        var sut = new Ledger();
        var date = new DateOnly(2000, 1, 1).FindFirstWeekday();
        var oldest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Sell, new DateOnly(2000, 1, 3),
                "TST", 10, 100M)
        };

        date = new DateOnly(2020, 1, 1).FindFirstWeekday();
        var newest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy, new DateOnly(2020, 1, 1),
                "TST", 30, 200M)
        };

        date = new DateOnly(2010, 1, 1).FindFirstWeekday();
        var middle = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Sell, new DateOnly(2010, 1, 1),
                "TST", 10, 100M)
        };

        sut.AddEntry(middle);
        sut.AddEntry(newest);
        sut.AddEntry(oldest);

        var positions = sut.GetPositions(newest.Date).ToArray();
        Assert.NotEmpty(positions);
        var lastPos = positions.Last();
        Assert.Equal(10, lastPos.Quantity);
        Assert.Equal(200M, lastPos.EntryPrice);
        Assert.Equal(newest.Date, lastPos.Date);
    }

    [Fact]
    public void GetPositionsForDateRange_EntriesForSameDateCombined()
    {
        var sut = new Ledger();
        var date = new DateOnly(2000, 1, 1).FindFirstWeekday();
        var oldest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Sell, new DateOnly(2000, 1, 3),
            "TST", 10, 100M)
        };

        date = new DateOnly(2020, 1, 1).FindFirstWeekday();
        var newest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy, new DateOnly(2020, 1, 1),
                "TST", 30, 200M)
        };

        date = new DateOnly(2010, 1, 1).FindFirstWeekday();
        var middle = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Sell, new DateOnly(2000, 1, 3),
            "TST", 10, 100M)
        };

        sut.AddEntry(middle);
        sut.AddEntry(newest);
        sut.AddEntry(oldest);

        var positions = sut.GetPositions(newest.Date).ToArray();
        Assert.NotEmpty(positions);
        var lastPos = positions.Last();
        Assert.Equal(10, lastPos.Quantity);
        Assert.Equal(200M, lastPos.EntryPrice);
        Assert.Equal(newest.Date, lastPos.Date);

        var pos = positions.FirstOrDefault(p => p.Date.Equals(middle.Date));
        Assert.Equal(-20, pos.Quantity);
        Assert.Equal(100M, pos.EntryPrice);
        Assert.Equal(-2000M, pos.LiquidValue);
    }

    [Fact]
    public void GetPositionsForDateRange_EntryPriceAdjusted()
    {
        var sut = new Ledger();
        var date = new DateOnly(2000, 1, 1).FindFirstWeekday();
        var oldest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy, new DateOnly(2000, 1, 3),
                "TST", 10, 100M)
        };

        date = new DateOnly(2020, 1, 1).FindFirstWeekday();
        var newest = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy, new DateOnly(2020, 1, 1),
                "TST", 20, 200M)
        };

        date = new DateOnly(2000, 1, 1).FindFirstWeekday();
        var middle = new LedgerEntry()
        {
            Date = date,
            Description = "Test",
            Transaction = new InstrumentTransaction(InstrumentTransactionType.Buy, new DateOnly(2000, 1, 3),
                "TST", 30, 300M)
        };

        sut.AddEntry(middle);
        sut.AddEntry(newest);
        sut.AddEntry(oldest);

        var positions = sut.GetPositions(newest.Date).ToArray();
        Assert.NotEmpty(positions);
        var lastPos = positions.Last();
        Assert.Equal(60, lastPos.Quantity);
        Assert.Equal(233.3333M, Math.Round(lastPos.EntryPrice, 4));
        Assert.Equal(newest.Date, lastPos.Date);
    }
}