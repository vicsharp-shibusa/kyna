using System.ComponentModel;
using System.Reflection;
using System.Text;

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

public static class DateTimeExtensions
{
    public static DateTime StartOfDay(this DateTime date) =>
        new(date.Year, date.Month, date.Day, 0, 0, 0, 0, date.Kind);

    public static DateTime EndOfDay(this DateTime date) =>
        new(date.Year, date.Month, date.Day, 23, 59, 59, 999, date.Kind);

    public static DateTime AddWeekdays(this DateTime dateTime, int numberToIncrement)
    {
        DateTime date = dateTime;
        if (numberToIncrement == 0) { return date; }

        int numberIncremented = 0;
        int increment = numberToIncrement > 0 ? 1 : -1;
        int positiveLimit = Math.Abs(numberToIncrement);

        while (numberIncremented < positiveLimit)
        {
            date = date.AddDays(increment);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) { continue; }
            numberIncremented++;
        }

        return date;
    }
}

public static class DateOnlyExtensions
{
    public static DateOnly AddWeekdays(this DateOnly dateOnly, int numberToIncrement)
    {
        DateOnly date = dateOnly;
        if (numberToIncrement == 0) { return date; }

        int numberIncremented = 0;
        int increment = numberToIncrement > 0 ? 1 : -1;
        int positiveLimit = Math.Abs(numberToIncrement);

        while (numberIncremented < positiveLimit)
        {
            date = date.AddDays(increment);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) { continue; }
            numberIncremented++;
        }

        return date;
    }
}


public static class StreamExtensions
{
    public static void Write(this Stream stream, string message)
    {
        if (stream.CanWrite && message != null)
        {
            ReadOnlySpan<byte> buffer = Encoding.UTF8.GetBytes(message);

            lock (stream)
            {
                stream.Write(buffer);
            }
        }
    }

    public static async Task WriteAsync(this Stream stream, string message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (stream.CanWrite && message != null && !cancellationToken.IsCancellationRequested)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            await stream.WriteAsync(buffer, cancellationToken);
        }
    }

    public static void WriteLine(this Stream stream, string? message = null) =>
        stream.Write($"{message ?? string.Empty}{Environment.NewLine}");

    public static async Task WriteLineAsync(this Stream stream, string? message = null,
        CancellationToken cancellationToken = default) =>
        await WriteAsync(stream, $"{message ?? string.Empty}{Environment.NewLine}", cancellationToken);
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
        var memberInfo = type.GetMember(enumerationValue.ToString());
        if (memberInfo.Length > 0)
        {
            var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attrs.Length > 0)
            {
                return ((DescriptionAttribute)attrs[0]).Description;
            }
        }
        return enumerationValue.ToString();
    }

    public static T GetEnumValueFromDescription<T>(this string text) where T : struct, Enum
    {
        var type = typeof(T);
        if (!type.IsEnum)
        {
            throw new ArgumentException($"{nameof(T)} must be of type Enum.");
        }
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
}