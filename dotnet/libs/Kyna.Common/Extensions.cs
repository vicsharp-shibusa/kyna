﻿using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kyna.Common;

public static class GuidExtensions
{
    public static string First8(this Guid guid) => guid.ToString()[..8];
}

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

    public static int CountWeekdays(this DateOnly dateOnly, DateOnly finishDate, bool includeFirstDay = false)
    {
        int days = 0;
        int factor = finishDate >= dateOnly ? 1 : -1;

        if (includeFirstDay && IsWeekday(dateOnly))
        {
            days += (1 * factor);
        }
        var d = dateOnly.AddDays(factor);
        while ((factor == 1 && d <= finishDate) || (factor == -1 && d >= finishDate))
        {
            if (IsWeekday(d))
            {
                days += (1 * factor);
            }
            d = d.AddDays(factor);
        }
        return days;
    }

    private static bool IsWeekday(DateOnly dateOnly)
    {
        if (dateOnly.DayOfWeek is DayOfWeek.Sunday or DayOfWeek.Saturday)
        {
            return false;
        }
        return true;
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

            await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
    }

    public static void WriteLine(this Stream stream, string? message = null) =>
        stream.Write($"{message ?? string.Empty}{Environment.NewLine}");

    public static async Task WriteLineAsync(this Stream stream, string? message = null,
        CancellationToken cancellationToken = default) =>
        await WriteAsync(stream, $"{message ?? string.Empty}{Environment.NewLine}", cancellationToken)
            .ConfigureAwait(false);
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
    }
}