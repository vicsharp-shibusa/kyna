﻿using System.ComponentModel;
using System.Text;

namespace Kyna.Common.Tests;

public class ExtensionTests
{
    [Fact]
    public void StartOfDay()
    {
        DateTime now = DateTime.Now;

        Assert.Equal(DateTimeKind.Local, now.Kind);
        DateTime sod = now.StartOfDay();
        Assert.NotEqual(now, sod);
        Assert.Equal(now.Kind, sod.Kind);
        Assert.Equal(0, sod.Hour);
        Assert.Equal(0, sod.Minute);
        Assert.Equal(0, sod.Second);
        Assert.Equal(0, sod.Millisecond);

        now = DateTime.UtcNow;
        Assert.Equal(DateTimeKind.Utc, now.Kind);
        sod = now.StartOfDay();
        Assert.NotEqual(now, sod);
        Assert.Equal(now.Kind, sod.Kind);
        Assert.Equal(0, sod.Hour);
        Assert.Equal(0, sod.Minute);
        Assert.Equal(0, sod.Second);
        Assert.Equal(0, sod.Millisecond);
    }

    [Fact]
    public void EndOfDay()
    {
        DateTime now = DateTime.Now;
        Assert.Equal(DateTimeKind.Local, now.Kind);
        DateTime eod = now.EndOfDay();
        Assert.NotEqual(now, eod);
        Assert.Equal(now.Kind, eod.Kind);
        Assert.Equal(23, eod.Hour);
        Assert.Equal(59, eod.Minute);
        Assert.Equal(59, eod.Second);
        Assert.Equal(999, eod.Millisecond);

        now = DateTime.UtcNow;
        Assert.Equal(DateTimeKind.Utc, now.Kind);
        eod = now.EndOfDay();
        Assert.NotEqual(now, eod);
        Assert.Equal(now.Kind, eod.Kind);
        Assert.Equal(23, eod.Hour);
        Assert.Equal(59, eod.Minute);
        Assert.Equal(59, eod.Second);
        Assert.Equal(999, eod.Millisecond);
    }

    [Fact]
    public void DateTime_AddWeekday_Forward_1()
    {
        DateTime saturday = new(2021, 10, 2);
        DateTime nextWeekday = saturday.AddWeekdays(1);
        DateTime expected = new(2021, 10, 4);
        Assert.Equal(expected, nextWeekday);
    }

    [Fact]
    public void DateTime_AddWeekday_Forward_10()
    {
        DateTime saturday = new(2021, 10, 2);
        DateTime nextWeekday = saturday.AddWeekdays(10);
        DateTime expected = new(2021, 10, 15);
        Assert.True(nextWeekday.DayOfWeek == DayOfWeek.Friday);
        Assert.Equal(expected, nextWeekday);
    }

    [Fact]
    public void DateTime_AddWeekday_Reverse_1()
    {
        DateTime saturday = new(2021, 10, 2);
        DateTime previousWeekday = saturday.AddWeekdays(-1);
        DateTime expected = new(2021, 10, 1);
        Assert.True(previousWeekday.DayOfWeek == DayOfWeek.Friday);
        Assert.Equal(expected, previousWeekday);
    }

    [Fact]
    public void DateTime_AddWeekday_Reverse_10()
    {
        DateTime saturday = new(2021, 10, 2);
        DateTime previousWeekday = saturday.AddWeekdays(-10);
        Assert.True(previousWeekday.DayOfWeek == DayOfWeek.Monday);
        DateTime expected = new(2021, 9, 20);
        Assert.Equal(expected, previousWeekday);
    }

    [Fact]
    public void DateOnly_AddWeekday_Forward_1()
    {
        DateOnly saturday = new(2021, 10, 2);
        DateOnly nextWeekday = saturday.AddWeekdays(1);
        DateOnly expected = new(2021, 10, 4);
        Assert.Equal(expected, nextWeekday);
    }

    [Fact]
    public void DateOnly_AddWeekday_Forward_10()
    {
        DateOnly saturday = new(2021, 10, 2);
        DateOnly nextWeekday = saturday.AddWeekdays(10);
        DateOnly expected = new(2021, 10, 15);
        Assert.True(nextWeekday.DayOfWeek == DayOfWeek.Friday);
        Assert.Equal(expected, nextWeekday);
    }

    [Fact]
    public void DateOnly_AddWeekday_Reverse_1()
    {
        DateOnly saturday = new(2021, 10, 2);
        DateOnly previousWeekday = saturday.AddWeekdays(-1);
        DateOnly expected = new(2021, 10, 1);
        Assert.True(previousWeekday.DayOfWeek == DayOfWeek.Friday);
        Assert.Equal(expected, previousWeekday);
    }

    [Fact]
    public void DateOnly_AddWeekday_Reverse_10()
    {
        DateOnly saturday = new(2021, 10, 2);
        DateOnly previousWeekday = saturday.AddWeekdays(-10);
        Assert.True(previousWeekday.DayOfWeek == DayOfWeek.Monday);
        DateOnly expected = new(2021, 9, 20);
        Assert.Equal(expected, previousWeekday);
    }

    private enum NoDescription
    {
        None = 0,
        Some,
        All
    }

    private enum WithDescription
    {
        [Description("Nothing")]
        None = 0,
        [Description("Some of it")]
        Some,
        [Description("All of it")]
        All
    }

    [Fact]
    public void GetDescription_WithDescription_GetsDescriptionValue()
    {
        Assert.Equal("Nothing", WithDescription.None.GetEnumDescription());
        Assert.Equal("Some of it", WithDescription.Some.GetEnumDescription());
        Assert.Equal("All of it", WithDescription.All.GetEnumDescription());
    }

    [Fact]
    public void GetDescription_NoDescription_GetStringValue()
    {
        Assert.Equal("None", NoDescription.None.GetEnumDescription());
        Assert.Equal("Some", NoDescription.Some.GetEnumDescription());
        Assert.Equal("All", NoDescription.All.GetEnumDescription());
    }

    [Fact]
    public void WriteToStream()
    {
        string message = "hello world";
        var stream = new MemoryStream();
        stream.Write(message);
        stream.Close();
        var buffer = stream.ToArray();
        var actual = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        Assert.Equal(message, actual);
    }

    [Fact]
    public async Task WriteToStreamAsync()
    {
        string message = "hello world";
        var stream = new MemoryStream();
        await stream.WriteAsync(message);
        stream.Close();
        var buffer = stream.ToArray();
        var actual = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        Assert.Equal(message, actual);
    }

    [Fact]
    public void WriteEmptyStringToStream()
    {
        string message = "";
        var stream = new MemoryStream();
        stream.Write(message);
        stream.Close();
        var buffer = stream.ToArray();
        var actual = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        Assert.Equal(message, actual);
    }

    [Fact]
    public async Task WriteEmptyStringToStreamAsync()
    {
        string message = "";
        var stream = new MemoryStream();
        await stream.WriteAsync(message);
        stream.Close();
        var buffer = stream.ToArray();
        var actual = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        Assert.Equal(message, actual);
    }

    [Fact]
    public void WriteLineToStream()
    {
        string message = "hello world";
        string expected = $"{message}{Environment.NewLine}";
        var stream = new MemoryStream();
        stream.WriteLine(message);
        stream.Close();
        var buffer = stream.ToArray();
        var actual = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task WriteLineToStreamAsync()
    {
        string message = "hello world";
        string expected = $"{message}{Environment.NewLine}";
        var stream = new MemoryStream();
        await stream.WriteLineAsync(message);
        stream.Close();
        var buffer = stream.ToArray();
        var actual = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WriteEmptyLineToStream()
    {
        string expected = Environment.NewLine;
        var stream = new MemoryStream();
        stream.WriteLine();
        stream.Close();
        var buffer = stream.ToArray();
        var actual = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task WriteEmptyLineToStreamAsync()
    {
        string expected = Environment.NewLine;
        var stream = new MemoryStream();
        await stream.WriteLineAsync();
        stream.Close();
        var buffer = stream.ToArray();
        var actual = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConvertToText_Hours()
    {
        TimeSpan ts = new(0, 3, 0, 0);
        string text = ts.ConvertToText();
        Assert.Contains("hours", text);
    }

    [Fact]
    public void ConvertToText_Minutes()
    {
        TimeSpan ts = new(0, 0, 40, 0);
        string text = ts.ConvertToText();
        Assert.Contains("minutes", text);
    }

    [Fact]
    public void ConvertToText_Seconds()
    {
        TimeSpan ts = new(0, 0, 0, 13);
        string text = ts.ConvertToText();
        Assert.Contains("seconds", text);
        Assert.DoesNotContain("millisecond", text);
    }

    [Fact]
    public void ConvertToText_Milliseconds()
    {
        TimeSpan ts = new(0, 0, 0, 0, 675);
        string text = ts.ConvertToText();
        Assert.Contains("milliseconds", text);
    }
}
