using Kyna.Backtests.Accounting;
using Kyna.Common;

namespace Kyna.Backtests.Tests.Accounting;

public class AccountTests
{
    [Fact]
    public void AccountDeposit_AccountSummary()
    {
        DateOnly date = new DateOnly(2020, 1, 4).FindFirstWeekday();
        decimal amount = 10_000M;

        var sut = new Account();
        var summary = sut.DepositFunds(date, amount);
        Assert.Equal(amount, summary.CashBalance);
        Assert.Equal(0M, summary.BookValue);
        Assert.Equal(0, summary.ReturnOnInvestment);
        Assert.Equal(0, summary.AnnualizedReturnOnInvestment);
        Assert.Equal(amount, summary.OwnersEquity);
        Assert.Equal(0, summary.TotalWithdrawn);
    }

    [Fact]
    public void WithdrawFunds_AccountSummary()
    {
        DateOnly date = new DateOnly(2020, 1, 4).FindFirstWeekday();
        decimal depositAmount = 10_000M;
        decimal withdrawalAmount = 1_000M;

        var sut = new Account();
        _ = sut.DepositFunds(date, depositAmount);
        date = date.AddDays(1);

        if (!sut.TryWithdrawFunds(date, withdrawalAmount, out var withdrawSummary))
        {
            Assert.Fail("Could not withdraw money.");
        }

        var expectedCash = depositAmount - withdrawalAmount;

        Assert.Equal(expectedCash, withdrawSummary.CashBalance);
        Assert.Equal(expectedCash, withdrawSummary.OwnersEquity);
        Assert.Equal(0, withdrawSummary.ReturnOnInvestment);
        Assert.Equal(0, withdrawSummary.AnnualizedReturnOnInvestment);
    }

    [Fact]
    public void BuyInstrument_AccountSummary()
    {
        DateOnly date = new DateOnly(2020, 1, 4).FindFirstWeekday();
        decimal depositAmount = 10_000M;

        var sut = new Account();
        _ = sut.DepositFunds(date, depositAmount);
        date = date.AddDays(1).FindFirstWeekday();

        if (!sut.TryBuyInstrument(date, "TEST", 100, 1M,
            out AccountSummary purchaseSummary, out string? message))
        {
            Assert.Fail(message);
        }

        var expectedCash = depositAmount - 100M;
        Assert.Equal(expectedCash, purchaseSummary.CashBalance);
        Assert.Equal(100M, purchaseSummary.BookValue);
    }

    [Fact]
    public void ShortInstrument_AccountSummary()
    {
        DateOnly date = new DateOnly(2020, 1, 4).FindFirstWeekday();
        decimal depositAmount = 10_000M;

        var sut = new Account();
        _ = sut.DepositFunds(date, depositAmount);
        date = date.AddDays(1).FindFirstWeekday();

        if (!sut.TrySellInstrument(date, "TEST", 100, 1M,
            out AccountSummary shortSummary, out string? message))
        {
            Assert.Fail(message);
        }

        var expectedCash = depositAmount + 100M;
        Assert.Equal(expectedCash, shortSummary.CashBalance);
        Assert.Equal(-100M, shortSummary.BookValue);
    }

    [Fact]
    public void AccountSummary_BuyAndSellInstrument_ReturnsNonZeroROI()
    {
        // Arrange
        DateOnly startDate = new DateOnly(2020, 1, 4).FindFirstWeekday(); // Let's say Jan 6, 2020 (Monday)
        decimal depositAmount = 10_000M;
        decimal buyPrice = 100M;
        int buyQuantity = 50; // Total cost: 5,000
        decimal sellPrice = 120M; // Total proceeds: 6,000
        DateOnly buyDate = startDate.AddDays(30); // Feb 5, 2020
        DateOnly sellDate = buyDate.AddDays(335); // Jan 6, 2021 (approx 1 year later)

        var sut = new Account();

        // Act
        _ = sut.DepositFunds(startDate, depositAmount);

        bool buySuccess = sut.TryBuyInstrument(buyDate, "TEST", buyQuantity, buyPrice,
            out _, out var buyMessage);
        Assert.True(buySuccess, buyMessage ?? "Buy failed");

        bool sellSuccess = sut.TrySellInstrument(sellDate, "TEST", buyQuantity, sellPrice,
            out var sellSummary, out var sellMessage);
        Assert.True(sellSuccess, sellMessage ?? "Sell failed");

        // Assert
        decimal expectedCash = depositAmount - (buyQuantity * buyPrice) + (buyQuantity * sellPrice); // 10,000 - 5,000 + 6,000 = 11,000
        decimal expectedProfit = (sellPrice - buyPrice) * buyQuantity; // (120 - 100) * 50 = 1,000
        decimal investmentCost = depositAmount; // 10,000
        double expectedROI = Convert.ToDouble(expectedProfit / investmentCost) * 100.0; // (1,000 / 10,000) * 100 = 10%

        double years = (sellDate.DayNumber - startDate.DayNumber) / 365.25; // ~1 year
        double expectedAnnualizedROI = (Math.Pow(1 + (double)(expectedProfit / investmentCost), 1 / years) - 1) * 100.0; // ~10%

        Assert.Equal(expectedCash, sellSummary.CashBalance);
        Assert.Equal(0M, sellSummary.BookValue); // After selling, no positions remain
        Assert.Equal(expectedCash, sellSummary.LiquidValue); // Cash + 0 positions
        Assert.Equal(depositAmount, sellSummary.TotalDeposited);
        Assert.Equal(0M, sellSummary.TotalWithdrawn);
        Assert.Equal(0M, sellSummary.InterestIncome);
        Assert.Equal(0M, sellSummary.DividendIncome);
        Assert.Equal(depositAmount, sellSummary.OwnersEquity); // No withdrawals

        // Allow for small floating-point differences
        Assert.Equal(expectedROI, sellSummary.ReturnOnInvestment, 4);
        Assert.Equal(expectedAnnualizedROI, sellSummary.AnnualizedReturnOnInvestment, 4);
    }
}
