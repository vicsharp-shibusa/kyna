namespace Kyna.EodHistoricalData;

/// <summary>
/// Represents information about the user to whom the API key belongs.
/// <seealso href="https://eodhistoricaldata.com/financial-apis/user-api/"/>
/// </summary>
public struct User
{
    public string Name;
    public string Email;
    public string SubscriptionType;
    public string PaymentMethod;
    public int ApiRequests;
    public DateOnly ApiRequestsDate;
    public int DailyRateLimit;
    public string? InviteToken;
    public int InviteTokenClicked;
}
