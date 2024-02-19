using Kyna.Analysis.Technical;

namespace Kyna.Analysis.Tests;

public class MovingAverageTests
{
    [Fact]
    public void MovingAverage_SimpleMovingAverage_CalculatesCorrectly()
    {
        var values = new decimal[] { 10M, 15M, 20M, 25M, 30M, 35M, 40M, 45M, 50M, 55M };
        var key = new MovingAverageKey(3, type: MovingAverageType.Simple);

        var movingAverage = new MovingAverage(key, values);

        Assert.Equal([0M, 0M, 15M, 20M, 25M, 30M, 35M, 40M, 45M, 50M], movingAverage.Values);
    }

    [Fact]
    public void MovingAverage_ExponentialMovingAverage_CalculatesCorrectly()
    {
        var values = new decimal[] { 10M, 15M, 20M, 25M, 30M, 35M, 40M, 45M, 50M, 55M };
        var key = new MovingAverageKey(3, type: MovingAverageType.Exponential);

        var movingAverage = new MovingAverage(key, values);

        var expected = new decimal[] { 0M, 0M, 15M, 20M, 25M, 30M, 35M, 40M, 45M, 50M, 55M };
        for (int i = 0; i < values.Length; i++)
        {
            Assert.Equal(expected[i], movingAverage.Values[i], 6);
        }
    }

    [Fact]
    public void MovingAverage_PeriodLessThanTwo_ReturnsZeroValues()
    {
        var values = new decimal[] { 10M, 15M, 20M, 25M, 30M };
        var key = new MovingAverageKey(1, type: MovingAverageType.Simple);

        var movingAverage = new MovingAverage(key, values);

        Assert.Equal([0M, 0M, 0M, 0M, 0M], movingAverage.Values);
    }
}
