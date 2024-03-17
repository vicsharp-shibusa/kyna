using Kyna.Analysis.Technical;
using Kyna.Common;
using System.Diagnostics;
using System.Text.Json;

namespace Kyna.Analysis.Tests;

public class ChartFactoryTests
{
    [Fact]
    public void WeeklyChart()
    {
        var aaplPricesFile = new FileInfo(Path.Combine("Data", "aapl_prices.json"));

        Assert.True(aaplPricesFile.Exists);

        var prices = JsonSerializer.Deserialize<EodHistoricalData.Models.PriceAction[]>(
            File.ReadAllText(aaplPricesFile.FullName),
            JsonOptionsRepository.DefaultSerializerOptions);

        Debug.Assert(prices != null);

        var ohlc = prices.Select(p => new Ohlc("AAPL", p.Date, p.Open, p.High,
            p.Low, p.Close, p.Volume)).ToArray();

        var weeklyChart = ChartFactory.Create("AAPL", null, null, ohlc,
            new ChartConfiguration()
            {
                Interval = "Weekly"
            });

        for (int i = 1; i < weeklyChart.Candlesticks.Length - 1; i++)
        {
            Assert.True(weeklyChart.Candlesticks[i].Date.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Tuesday);
            Assert.True(weeklyChart.Candlesticks[i].End.DayOfWeek is DayOfWeek.Thursday or DayOfWeek.Friday);
        }
    }

    [Fact]
    public void MonthlyChart()
    {
        var aaplPricesFile = new FileInfo(Path.Combine("Data", "aapl_prices.json"));

        Assert.True(aaplPricesFile.Exists);

        var prices = JsonSerializer.Deserialize<EodHistoricalData.Models.PriceAction[]>(
            File.ReadAllText(aaplPricesFile.FullName),
            JsonOptionsRepository.DefaultSerializerOptions);

        Debug.Assert(prices != null);

        var ohlc = prices.Select(p => new Ohlc("AAPL", p.Date, p.Open, p.High,
            p.Low, p.Close, p.Volume)).ToArray();

        var monthlyChart = ChartFactory.Create("AAPL", null, null, ohlc,
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

        var prices = JsonSerializer.Deserialize<EodHistoricalData.Models.PriceAction[]>(
            File.ReadAllText(aaplPricesFile.FullName),
            JsonOptionsRepository.DefaultSerializerOptions);

        Debug.Assert(prices != null);

        var ohlc = prices.Select(p => new Ohlc("AAPL", p.Date, p.Open, p.High,
            p.Low, p.Close, p.Volume)).ToArray();

        var quarterlyChart = ChartFactory.Create("AAPL", null, null, ohlc,
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

        var prices = JsonSerializer.Deserialize<EodHistoricalData.Models.PriceAction[]>(
            File.ReadAllText(aaplPricesFile.FullName),
            JsonOptionsRepository.DefaultSerializerOptions);

        Debug.Assert(prices != null);

        var ohlc = prices.Select(p => new Ohlc("AAPL", p.Date, p.Open, p.High,
            p.Low, p.Close, p.Volume)).ToArray();

        var annualChart = ChartFactory.Create("AAPL", null, null, ohlc,
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

        var prices = JsonSerializer.Deserialize<EodHistoricalData.Models.PriceAction[]>(
            File.ReadAllText(aaplPricesFile.FullName),
            JsonOptionsRepository.DefaultSerializerOptions);

        Debug.Assert(prices != null);

        var ohlc1 = prices.Select(p => new Ohlc("AAPL", p.Date, p.Open, p.High,
            p.Low, p.Close, p.Volume)).ToArray();
        var ohlc2 = prices.Select(p => new Ohlc("TEST", p.Date, p.Open, p.High,
            p.Low, p.Close, p.Volume)).ToArray();

        var chart = ChartFactory.Create("Market", null, ohlc1, ohlc2);
    }
}
