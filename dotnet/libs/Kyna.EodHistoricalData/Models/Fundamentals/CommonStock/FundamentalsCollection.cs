namespace Kyna.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct FundamentalsCollection
{
    public General General;
    public Highlights Highlights;
    public Valuation Valuation;
    public SharesStats SharesStats;
    public Technicals Technicals;
    public SplitsDividends SplitsDividends;
    public AnalystRatings AnalystRatings;
    public Holders? Holders;
    public IDictionary<string, InsiderTransaction>? InsiderTransactions;
    public EsgScores EsgScores;
    public OutstandingShares OutstandingShares;
    public Earnings Earnings;
    public Financials Financials;
}
