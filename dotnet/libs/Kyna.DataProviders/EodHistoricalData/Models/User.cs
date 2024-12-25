namespace Kyna.DataProviders.EodHistoricalData.Models;

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
