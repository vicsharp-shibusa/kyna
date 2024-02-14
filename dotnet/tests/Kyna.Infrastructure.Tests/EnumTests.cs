using Xunit.Abstractions;

namespace Kyna.Infrastructure.Tests;

public class EnumTests
{
    private readonly ITestOutputHelper _output;

    public EnumTests(ITestOutputHelper output)
    {
        _output = output;
    }

    //[Fact]
    //public void MigrationAdjustedPriceModes_HasFlags()
    //{
    //    var adjMode = AdjustedPriceModes.OnlySplits | AdjustedPriceModes.DeleteFromSource;

    //    Assert.True(adjMode.HasFlag(AdjustedPriceModes.OnlySplits));
    //    Assert.True(adjMode.HasFlag(AdjustedPriceModes.DeleteFromSource));
    //    Assert.False(adjMode == AdjustedPriceModes.None);

    //    _output.WriteLine(adjMode.GetEnumDescription());
    //}

    //[Fact]
    //public void MigrationAdjustedPriceModes_FromDescriptionToEnum()
    //{
    //    var adjMode = AdjustedPriceModes.OnlySplits | AdjustedPriceModes.DeleteFromSource;

    //    Assert.True(adjMode.HasFlag(AdjustedPriceModes.OnlySplits));
    //    Assert.True(adjMode.HasFlag(AdjustedPriceModes.DeleteFromSource));
    //    Assert.False(adjMode == AdjustedPriceModes.None);

    //    _output.WriteLine(adjMode.GetEnumDescription());
    //}
}
