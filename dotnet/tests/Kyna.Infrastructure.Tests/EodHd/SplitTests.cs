using Kyna.Common;
using Kyna.Infrastructure.Database.DataAccessObjects;
using System.Diagnostics;
using System.Text.Json;
using Xunit.Abstractions;

namespace Kyna.Infrastructure.Tests.EodHd;

public class SplitTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void SplitCalc_Aapl()
    {
        var aaplPricesFile = new FileInfo(Path.Combine("EodHd", "Mocks", "aapl_prices.json"));
        var applSplitsFile = new FileInfo(Path.Combine("EodHd", "Mocks", "aapl_splits.json"));

        Assert.True(aaplPricesFile.Exists);
        Assert.True(applSplitsFile.Exists);

        var splits = JsonSerializer.Deserialize<EodHistoricalData.Split[]>(File.ReadAllText(applSplitsFile.FullName),
            JsonOptionsRepository.DefaultSerializerOptions);
        var prices = JsonSerializer.Deserialize<EodHistoricalData.PriceAction[]>(File.ReadAllText(aaplPricesFile.FullName),
            JsonOptionsRepository.DefaultSerializerOptions);

        Assert.NotNull(splits);
        Assert.NotEmpty(splits);

        Assert.NotNull(prices);
        Assert.NotEmpty(prices);

        var daoPrices = new EodPrice[prices.Length];

        for (int i = 0; i < prices.Length; i++)
        {
            daoPrices[i] = new EodPrice("eodhd.com", "AAPL.US", null)
            {
                Open = prices[i].Open,
                High = prices[i].High,
                Low = prices[i].Low,
                Close = prices[i].Close,
                Volume = prices[i].Volume,
                DateEod = prices[i].Date
            };
        }

        var daoSplits = new Split[splits.Length];

        for (int i = 0; i < splits.Length; i++)
        {
            Debug.Assert(splits[i].SplitText != null);

            var (Before, After) = SplitAdjustedPriceCalculator.ConvertFromText(splits[i].SplitText!);
            daoSplits[i] = new Split("eodhd.com", "AAPL.US")
            {
                After = After,
                Before = Before,
                SplitDate = splits[i].Date
            };
        }

        var adjPrices = SplitAdjustedPriceCalculator.Calculate(daoPrices, daoSplits).ToArray();

        Assert.NotNull(adjPrices);
        Assert.NotEmpty(adjPrices);

        Assert.Equal(prices.Length, adjPrices.Length);

        var split2014 = adjPrices.FirstOrDefault(p => p.DateEod.Equals(new DateOnly(2014, 6, 9)));
        Assert.NotNull(split2014);
        _output.WriteLine($"{adjPrices.First().DateEod:yyyy-MM-dd} {adjPrices.First().Close}");
        _output.WriteLine($"{split2014.DateEod:yyyy-MM-dd} {split2014?.Close}");
        _output.WriteLine($"{adjPrices.Last().DateEod:yyyy-MM-dd} {adjPrices.Last().Close}");

        /*
         * Note: I spot-checked these values against Yahoo here:
         * https://finance.yahoo.com/quote/AAPL/history
         * They are all within a penny.
         */
    }
}
