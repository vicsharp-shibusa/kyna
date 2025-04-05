using System.Text;

namespace Kyna.Backtests.Accounting;

internal readonly record struct AccountSummary
{
    public AccountSummary(Guid id,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        decimal cashBalance = 0M, 
        int numberTransactions = 0,
        decimal bookValue = 0M, 
        decimal liquidValue = 0M,
        decimal totalDeposited = 0M, 
        decimal totalWithdrawn = 0M,
        decimal interestIncome = 0M,
        decimal dividendIncome = 0M)
    {
        Id = id;
        StartDate = startDate ?? new DateOnly(1900, 1, 1);
        EndDate = endDate ?? DateOnly.FromDateTime(DateTime.Now);
        CashBalance = cashBalance;
        NumberTransactions = numberTransactions;
        BookValue = bookValue;
        LiquidValue = liquidValue;
        TotalDeposited = totalDeposited;
        TotalWithdrawn = totalWithdrawn;
        InterestIncome = interestIncome;
        DividendIncome = dividendIncome;
    }

    public Guid Id { get; }
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }
    public decimal CashBalance { get; }
    public int NumberTransactions { get; }
    public decimal BookValue { get; }
    public decimal LiquidValue { get; }
    public decimal TotalDeposited { get; }
    public decimal TotalWithdrawn { get; }
    public decimal InterestIncome { get; }
    public decimal DividendIncome { get; }

    public decimal OwnersEquity => TotalDeposited - TotalWithdrawn;

    public double ReturnOnInvestment
    {
        get
        {
            // True invested amount (excluding cash on hand)
            decimal investedAmount = TotalDeposited - TotalWithdrawn - CashBalance;
            // True profit (only investment gains)
            decimal investmentValue = LiquidValue - CashBalance; // Remove cash from LiquidValue
            decimal netProfit = investmentValue + InterestIncome + DividendIncome - investedAmount;
            decimal investmentCost = TotalDeposited;

            if (investmentCost <= 0)
            {
                return netProfit > 0 ? double.PositiveInfinity : 0;
            }

            return Convert.ToDouble(netProfit / investmentCost) * 100.0;
        }
    }

    public double AnnualizedReturnOnInvestment
    {
        get
        {
            decimal investedAmount = TotalDeposited - TotalWithdrawn - CashBalance;  // 0
            decimal investmentValue = LiquidValue - CashBalance;  // 9,000 - 9,000 = 0
            decimal netProfit = investmentValue + InterestIncome + DividendIncome - investedAmount;  // 0 + 0 + 0 - 0 = 0
            decimal investmentCost = TotalDeposited;  // 10,000

            if (investmentCost <= 0)
                return netProfit > 0 ? double.PositiveInfinity : 0;

            double years = (EndDate.DayNumber - StartDate.DayNumber) / 365.25;
            if (years <= 0)
                return 0;

            double totalReturn = Convert.ToDouble(netProfit / investmentCost);  // 0 / 10,000 = 0
            return (Math.Pow(1 + totalReturn, 1 / years) - 1) * 100.0;  // (1 + 0)^(anything) - 1 * 100 = 0
        }
    }

    public decimal TotalProfit => (LiquidValue + CashBalance) + TotalWithdrawn - TotalDeposited +
                             InterestIncome + DividendIncome;

    public double TimeInYears => (EndDate.DayNumber - StartDate.DayNumber) / 365.25;

    public override string ToString()
    {
        StringBuilder result = new();

        TimeSpan ts = new(days: EndDate.DayNumber - StartDate.DayNumber, 0, 0, 0);
        var months = Convert.ToInt32(Math.Floor(ts.TotalDays / (365D / 12D)));
        result.AppendLine($"\t{Id}");
        result.AppendLine($"\t\tStart Date           : {StartDate:yyyy-MM-dd}");
        result.AppendLine($"\t\tEnd Date             : {EndDate:yyyy-MM-dd}");
        result.AppendLine($"\t\tDuration             : {months} months");
        result.AppendLine($"\t\tNumber Transactions  : {NumberTransactions}");
        result.AppendLine($"\t\tCash                 : {CashBalance:C}");
        result.AppendLine($"\t\tBook Value           : {BookValue:C}");
        result.AppendLine($"\t\tLiquid Value         : {LiquidValue:C}");
        result.AppendLine($"\t\tTotal Deposited      : {TotalDeposited:C}");
        result.AppendLine($"\t\tTotal Withdrawn      : {TotalWithdrawn:C}");
        result.AppendLine($"\t\tInterest Income      : {InterestIncome:C}");
        result.AppendLine($"\t\tDividend Income      : {DividendIncome:C}");
        result.AppendLine($"\t\tOwner's Equity       : {OwnersEquity:C}");
        result.AppendLine($"\t\tReturn on Investment : {ReturnOnInvestment:#,##0.0000}");

        return result.ToString();
    }
}