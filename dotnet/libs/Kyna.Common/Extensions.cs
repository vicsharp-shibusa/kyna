using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Kyna.Common;

public static class GuidExtensions
{
    public static string First(this Guid guid, int number = 8) =>
        guid.ToString("N")[..Math.Min(Math.Max(0, number), 32)];
}

public static class TimeSpanExtensions
{
    public static string ConvertToText(this TimeSpan timeSpan)
    {
        double value;
        string unit;
        if (Math.Abs(timeSpan.TotalHours) >= 1D)
        {
            value = timeSpan.TotalHours;
            unit = "hours";
        }
        else if (Math.Abs(timeSpan.TotalMinutes) >= 1D)
        {
            value = timeSpan.TotalMinutes;
            unit = "minutes";
        }
        else if (Math.Abs(timeSpan.TotalSeconds) >= 1D)
        {
            value = timeSpan.TotalSeconds;
            unit = "seconds";
        }
        else
        {
            value = timeSpan.TotalMilliseconds;
            unit = "milliseconds";
        }
        return $"{value:0.##} {unit}";
    }
}

public static class DateTimeExtensions
{
    public static DateTime StartOfDay(this DateTime date) => date.Date;
    public static DateTime EndOfDay(this DateTime date) =>
        date.Date.AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);

    public static DateTime AddWeekdays(this DateTime dateTime, int numberToIncrement)
    {
        if (numberToIncrement == 0)
            return dateTime;

        int increment = Math.Sign(numberToIncrement);
        int remaining = Math.Abs(numberToIncrement);
        DateTime date = dateTime;

        while (remaining > 0)
        {
            date = date.AddDays(increment);
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                remaining--;
            }
        }
        return date;
    }
}

public static class DateOnlyExtensions
{
    public static DateOnly AddWeekdays(this DateOnly dateOnly, int numberToIncrement)
    {
        DateOnly date = dateOnly;
        if (numberToIncrement == 0)
        {
            return date;
        }

        int numberIncremented = 0;
        int increment = numberToIncrement > 0 ? 1 : -1;
        int positiveLimit = Math.Abs(numberToIncrement);

        while (numberIncremented < positiveLimit)
        {
            date = date.AddDays(increment);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }
            numberIncremented++;
        }

        return date;
    }

    public static int CountWeekdays(this DateOnly startDate, DateOnly endDate, bool includeFirstDay = false)
    {
        if (startDate == endDate)
            return includeFirstDay && IsWeekday(startDate) ? 1 : 0;

        int totalDays = Math.Abs(endDate.DayNumber - startDate.DayNumber) + (includeFirstDay ? 1 : 0);
        if (totalDays == 0)
            return 0;

        DateOnly start = includeFirstDay ? startDate : startDate.AddDays(endDate > startDate ? 1 : -1);
        DateOnly end = endDate;

        int weeks = totalDays / 7;
        int remainder = totalDays % 7;
        int weekdays = weeks * 5; // 5 weekdays per week

        DateOnly current = start;
        for (int i = 0; i < remainder; i++)
        {
            if (IsWeekday(current))
                weekdays += endDate >= startDate ? 1 : -1;
            current = current.AddDays(endDate >= startDate ? 1 : -1);
        }

        return weekdays;
    }

    private static bool IsWeekday(DateOnly date) =>
        date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
}

/// <summary>
/// Represents a set of <see cref="Stream"/> extensions to simplify writing text to the stream.
/// </summary>
public static class StreamExtensions
{
    private static readonly Lock _writeLock = new();

    /// <summary>
    /// Writes a message to the stream if it's writable.
    /// </summary>
    public static void Write(this Stream stream, string? message)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));
        if (!string.IsNullOrEmpty(message) && stream.CanWrite)
        {
            lock (_writeLock)
            {
                stream.Write(Encoding.UTF8.GetBytes(message));
            }
        }
    }

    /// <summary>
    /// Writes a message asynchronously to the stream if it's writable.
    /// </summary>
    public static ValueTask WriteAsync(this Stream stream, string message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrEmpty(message) && !cancellationToken.IsCancellationRequested && (stream?.CanWrite ?? false))
        {
            return stream.WriteAsync(Encoding.UTF8.GetBytes(message), cancellationToken);
        }

        return default;
    }

    /// <summary>
    /// Writes a message with a newline at the end.
    /// </summary>
    public static void WriteLine(this Stream stream, string message = null)
    {
        Write(stream, $"{message ?? string.Empty}{Environment.NewLine}");
    }

    /// <summary>
    /// Writes a message with a newline asynchronously.
    /// </summary>
    public static ValueTask WriteLineAsync(this Stream stream, string? message = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return WriteAsync(stream, $"{message ?? string.Empty}{Environment.NewLine}", cancellationToken);
    }
}

public static class EnumExtensions
{
    public static string GetEnumDescription<T>(this T enumerationValue) where T : struct, Enum
    {
        var type = enumerationValue.GetType();
        if (!type.IsEnum)
        {
            throw new ArgumentException($"{nameof(T)} must be of type Enum.");
        }

        bool hasFlagsAttribute = Attribute.IsDefined(type, typeof(FlagsAttribute));

        var memberInfo = type.GetMember(enumerationValue.ToString());

        if (hasFlagsAttribute)
        {
            List<string> results = new(10);
            foreach (T enumVal in Enum.GetValues(type).Cast<T>().Where(i => Convert.ToInt32(i) > 0))
            {
                if (enumerationValue.HasFlag(enumVal))
                {
                    var info = type.GetMember(enumVal.ToString());
                    if (info.Length > 0)
                    {
                        var attrs = info[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                        if (attrs.Length > 0)
                        {
                            results.Add(((DescriptionAttribute)attrs[0]).Description);
                        }
                        else
                        {
                            results.Add(enumVal.ToString());
                        }
                    }
                }
            }

            return string.Join(", ", results);
        }

        if (memberInfo.Length > 0)
        {
            var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attrs.Length > 0)
            {
                return ((DescriptionAttribute)attrs[0]).Description;
            }
            else
            {
                return enumerationValue.ToString();
            }
        }

        return default(T).ToString();
    }

    public static T GetEnumValueFromDescription<T>(this string text) where T : struct, Enum
    {
        var type = typeof(T);
        if (!type.IsEnum)
        {
            throw new ArgumentException($"{nameof(T)} must be of type Enum.");
        }

        if (text.Contains(','))
        {
            List<string> strValues = new(10);
            string[] split = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var str in split)
            {
                MemberInfo[] members = type.GetMembers(BindingFlags.Public | BindingFlags.Static);
                foreach (MemberInfo member in members)
                {
                    var attrs = member.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    if (attrs.Length > 0)
                    {
                        for (int i = 0; i < attrs.Length; i++)
                        {
                            string description = ((DescriptionAttribute)attrs[i]).Description;
                            if (str.Equals(description, StringComparison.OrdinalIgnoreCase))
                            {
                                strValues.Add(((T)Enum.Parse(type, member.Name, true)).ToString());
                            }
                        }
                    }
                    else if (member.Name.Equals(str, StringComparison.OrdinalIgnoreCase))
                    {
                        strValues.Add(((T)Enum.Parse(type, member.Name, true)).ToString());
                    }
                }
            }

            return (T)Enum.Parse(type, string.Join(", ", strValues));
        }
        else
        {
            MemberInfo[] members = type.GetMembers(BindingFlags.Public | BindingFlags.Static);
            foreach (MemberInfo member in members)
            {
                var attrs = member.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs.Length > 0)
                {
                    for (int i = 0; i < attrs.Length; i++)
                    {
                        string description = ((DescriptionAttribute)attrs[i]).Description;
                        if (text.Equals(description, StringComparison.OrdinalIgnoreCase))
                        {
                            return (T)Enum.Parse(type, member.Name, true);
                        }
                    }
                }
                if (member.Name.Equals(text, StringComparison.OrdinalIgnoreCase))
                {
                    return (T)Enum.Parse(type, member.Name, true);
                }
            }

            return default;
        }

        throw new ArgumentException($"No enum value found for description '{text}'.", nameof(text));
    }
}