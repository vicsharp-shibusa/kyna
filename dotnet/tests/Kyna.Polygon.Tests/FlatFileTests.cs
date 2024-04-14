using Kyna.Polygon.Models;
using Xunit.Abstractions;

namespace Kyna.Polygon.Tests;

public class FlatFileTests
{
    private readonly ITestOutputHelper _output;
    public FlatFileTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ParseLineText()
    {
        string line = "A,1111999,139.23,137.52,139.885,136.71,1714622400000000000,25515";
        var sut = new FlatFile(line);

        Assert.Equal(sut.Date, new DateOnly(2024, 5, 2));
        Assert.Equal("A", sut.Code);
        Assert.Equal(1111999, sut.Volume);
        Assert.Equal(139.23M, sut.Open);
        Assert.Equal(137.52M, sut.Close);
        Assert.Equal(139.885M, sut.High);
        Assert.Equal(136.71M, sut.Low);
    }

    //[Fact]
    //public void Test1()
    //{
    //    var num = 1714622400000000000;

    //    var x = num / 1000 / 60 / 60 / 24 / 365;

    //    //var b = new DateTime(1970,1,1,0,0,0, DateTimeKind.Utc);
    //    //var d = b.AddMilliseconds(num);
    //    //var c = b.AddTicks(num);
    //    _output.WriteLine(x.ToString());
    //}
}
