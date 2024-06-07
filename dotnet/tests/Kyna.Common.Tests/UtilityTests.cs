using System.ComponentModel;

namespace Kyna.Common.Tests;

public class UtilityTests
{
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
    public void GetDescriptions_WithDescription_GetsDescriptionValues()
    {
        List<string> descriptions = ["Nothing", "Some of it", "All of it"];
        Assert.True(descriptions.SequenceEqual(EnumUtilities.GetDescriptions<WithDescription>()));
    }

    [Fact]
    public void GetDescriptions_NoDescription_GetsStringValues()
    {
        List<string> descriptions = ["None", "Some", "All"];
        Assert.True(descriptions.SequenceEqual(EnumUtilities.GetDescriptions<NoDescription>()));
    }

    [Fact]
    public void ENotationTest()
    {
        string x = "9.999999747378752e-05";
        var r = decimal.TryParse(x, System.Globalization.NumberStyles.Float, null, out decimal result);
        Assert.True(r);
        Assert.Equal(0.00009999999747378752M, result);
    }
}
