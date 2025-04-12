using Kyna.Analysis.Technical;
using Kyna.Analysis.Technical.Charts;
using Kyna.Common;
using System.Diagnostics;
using System.Text.Json;

namespace Kyna.Analysis.Tests;

public class ChartFactoryTests
{
    public record StockData(
        DateOnly Date,
        decimal Open,
        decimal High,
        decimal Low,
        decimal Close,
        decimal AdjustedClose,
        long Volume);

    [Fact]
    public void WeeklyChart()
    {
        var aaplPricesFile = new FileInfo(Path.Combine("Data", "aapl_prices.json"));

        Assert.True(aaplPricesFile.Exists);

        var prices = JsonSerializer.Deserialize<StockData[]>(
            File.ReadAllText(aaplPricesFile.FullName),
            JsonSerializerOptionsRepository.Custom);

        Debug.Assert(prices != null);

        var ohlc = prices.Select(p => new Ohlc("AAPL", p.Date, p.Open, p.High,
            p.Low, p.Close, p.Volume)).ToArray();

        var weeklyChart = ChartFactory.Create("TEST", "AAPL", null, null, ohlc,
            new ChartConfiguration()
            {
                Interval = "Weekly"
            });

        for (int i = 1; i < weeklyChart.Candlesticks.Length - 1; i++)
        {
            var sow = weeklyChart.Candlesticks[i].Start;
            var eow = weeklyChart.Candlesticks[i].End;
            Assert.True(sow < eow);
            Assert.True(sow.DayOfWeek < eow.DayOfWeek);
        }
    }

    [Fact]
    public void MonthlyChart()
    {
        var aaplPricesFile = new FileInfo(Path.Combine("Data", "aapl_prices.json"));

        Assert.True(aaplPricesFile.Exists);

        var prices = JsonSerializer.Deserialize<StockData[]>(
            File.ReadAllText(aaplPricesFile.FullName),
            JsonSerializerOptionsRepository.Custom);

        Debug.Assert(prices != null);

        var ohlc = prices.Select(p => new Ohlc("AAPL", p.Date, p.Open, p.High,
            p.Low, p.Close, p.Volume)).ToArray();

        var monthlyChart = ChartFactory.Create("TEST", "AAPL", null, null, ohlc,
            new ChartConfiguration()
            {
                Interval = "Monthly"
            });

        var month = monthlyChart.Candlesticks[0].Date.Month;
        for (int i = 1; i < monthlyChart.Candlesticks.Length - 1; i++)
        {
            Assert.True(monthlyChart.Candlesticks[i].Date.Day < 5);
            Assert.True(monthlyChart.Candlesticks[i].Date.Month != month);
            month = monthlyChart.Candlesticks[i].Date.Month;
        }
    }

    [Fact]
    public void QuarterlyChart()
    {
        var aaplPricesFile = new FileInfo(Path.Combine("Data", "aapl_prices.json"));

        Assert.True(aaplPricesFile.Exists);

        var prices = JsonSerializer.Deserialize<StockData[]>(
            File.ReadAllText(aaplPricesFile.FullName),
            JsonSerializerOptionsRepository.Custom);

        Debug.Assert(prices != null);

        var ohlc = prices.Select(p => new Ohlc("AAPL", p.Date, p.Open, p.High,
            p.Low, p.Close, p.Volume)).ToArray();

        var quarterlyChart = ChartFactory.Create("TEST", "AAPL", null, null, ohlc,
            new ChartConfiguration()
            {
                Interval = "Quarterly"
            });

        var month = quarterlyChart.Candlesticks[0].Date.Month;
        for (int i = 1; i < quarterlyChart.Candlesticks.Length - 1; i++)
        {
            Assert.True(quarterlyChart.Candlesticks[i].Date.Month is 1 or 4 or 7 or 10);
            Assert.True(quarterlyChart.Candlesticks[i].Date.Day < 5);
            month = quarterlyChart.Candlesticks[i].Date.Month;
        }
    }

    [Fact]
    public void AnnualChart()
    {
        var aaplPricesFile = new FileInfo(Path.Combine("Data", "aapl_prices.json"));

        Assert.True(aaplPricesFile.Exists);

        var prices = JsonSerializer.Deserialize<StockData[]>(
            File.ReadAllText(aaplPricesFile.FullName),
            JsonSerializerOptionsRepository.Custom);

        Debug.Assert(prices != null);

        var ohlc = prices.Select(p => new Ohlc("AAPL", p.Date, p.Open, p.High,
            p.Low, p.Close, p.Volume)).ToArray();

        var annualChart = ChartFactory.Create("TEST", "AAPL", null, null, ohlc,
            new ChartConfiguration()
            {
                Interval = "Annually"
            });

        var year = annualChart.Candlesticks[0].Date.Year;
        for (int i = 1; i < annualChart.Candlesticks.Length - 1; i++)
        {
            Assert.Equal(1, annualChart.Candlesticks[i].Date.Month);
            Assert.True(annualChart.Candlesticks[i].Date.Year > year);
            year = annualChart.Candlesticks[i].Date.Year;
        }
    }

    [Fact]
    public void CombinedChart()
    {
        var aaplPricesFile = new FileInfo(Path.Combine("Data", "aapl_prices.json"));

        Assert.True(aaplPricesFile.Exists);

        var prices = JsonSerializer.Deserialize<StockData[]>(
            File.ReadAllText(aaplPricesFile.FullName),
            JsonSerializerOptionsRepository.Custom);

        Debug.Assert(prices != null);

        var ohlc1 = prices.Select(p => new Ohlc("AAPL", p.Date, p.Open, p.High,
            p.Low, p.Close, p.Volume)).ToArray();
        var ohlc2 = prices.Select(p => new Ohlc("TEST", p.Date, p.Open, p.High,
            p.Low, p.Close, p.Volume)).ToArray();

        var chart = ChartFactory.Create("TEST", "Market", null, null, null, ohlc1, ohlc2);
    }
}
