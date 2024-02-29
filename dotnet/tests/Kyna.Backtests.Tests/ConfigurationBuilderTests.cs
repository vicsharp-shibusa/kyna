using Kyna.Analysis.Technical;
using Kyna.Common;
using System.Text.Json;

namespace Kyna.Backtests.Tests;

public class ConfigurationBuilderTests
{
    [Fact]
    public void Builder()
    {
        var config = new BacktestingConfiguration(BacktestType.RandomBaseline,
            "eodhd.com", "Baseline 1", "Testing", PricePoint.Close,
            new TestTargetPercentage(PricePoint.High, .1),
            new TestTargetPercentage(PricePoint.Low, .1));

        var options = JsonOptionsRepository.DefaultSerializerOptions;
        options.Converters.Add(new EnumDescriptionConverter<BacktestType>());
        options.Converters.Add(new EnumDescriptionConverter<PricePoint>());

        var json = JsonSerializer.Serialize(config, options);

        Assert.NotNull(json);
    }
}