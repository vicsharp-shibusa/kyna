namespace Kyna.DataProviders.EodHistoricalData.Models.Fundamentals.CommonStock;

/// <summary>
/// <seealso href="https://eodhistoricaldata.com/financial-apis/stock-etfs-fundamental-data-feeds/"/>
/// </summary>
public struct EsgScores
{
    public string? Disclaimer;
    public DateOnly? RatingDate;
    public double? TotalEsg;
    public double? TotalEsgPercentile;
    public double? EnvironmentScore;
    public double? EnvironmentScorePercentile;
    public double? SocialScore;
    public double? SocialScorePercentile;
    public double? GovernanceScore;
    public double? GovernanceScorePercentile;
    public double? ControversyLevel;
    public IDictionary<string, ActivityInvolvement>? ActivitiesInvolvement;
}
