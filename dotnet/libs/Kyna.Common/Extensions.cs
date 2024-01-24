namespace Kyna.Common;

public static class TimeSpanExtensions
{
    public static string ConvertToText(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours > 1D)
        {
            return $"{timeSpan.TotalHours:##0.00} hours";
        }
        else if (timeSpan.TotalMinutes > 1D)
        {
            return $"{timeSpan.TotalMinutes:##0.00} minutes";
        }
        else if (timeSpan.TotalSeconds > 1D)
        {
            return $"{timeSpan.TotalSeconds:##0.00} seconds";
        }
        else
        {
            return $"{timeSpan.TotalMilliseconds:##0.00} milliseconds";
        }
    }
}