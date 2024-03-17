using Kyna.Analysis.Technical;
using Kyna.Common;
using System.Text.Json.Serialization;

namespace Kyna.Backtests;

public struct TestTargetPercentage(PricePoint pricePoint, double value)
{
    [JsonPropertyName("Price Point")]
    public PricePoint PricePoint = pricePoint;
    public double Value = value;
    public override readonly string ToString()
    {
        return $"{Value * 100}% on {PricePoint.GetEnumDescription()}";
    }
}
