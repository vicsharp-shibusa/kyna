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
        List<string> descriptions = new() { "Nothing", "Some of it", "All of it" };
        Assert.True(descriptions.SequenceEqual(EnumUtilities.GetDescriptions<WithDescription>()));
    }

    [Fact]
    public void GetDescriptions_NoDescription_GetsStringValues()
    {
        List<string> descriptions = new() { "None", "Some", "All" };
        Assert.True(descriptions.SequenceEqual(EnumUtilities.GetDescriptions<NoDescription>()));
    }
}
