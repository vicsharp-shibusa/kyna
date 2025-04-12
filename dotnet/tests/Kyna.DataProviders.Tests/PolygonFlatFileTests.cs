using Kyna.DataProviders.Polygon.Models;
using Xunit.Abstractions;

namespace Kyna.Polygon.Tests;

public class PolygonFlatFileTests
{
    private readonly ITestOutputHelper _output;
    public PolygonFlatFileTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ParseLineText()
    {
        string line = "A,1111999,139.23,137.52,139.885,136.71,1714622400000000000,25515";
        var sut = new FlatFileLine(line);

        Assert.Equal(sut.Date, new DateOnly(2024, 5, 2));
        Assert.Equal("A", sut.Code);
        Assert.Equal(1111999, sut.Volume);
        Assert.Equal(139.23M, sut.Open);
        Assert.Equal(137.52M, sut.Close);
        Assert.Equal(139.885M, sut.High);
        Assert.Equal(136.71M, sut.Low);
    }
}
