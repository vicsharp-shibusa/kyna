using Kyna.Analysis.Technical;
using Kyna.Common;

namespace Kyna.Backtests;

public struct TestTargetPercentage(PricePoint pricePoint, double value)
{
    public PricePoint PricePoint = pricePoint;
    public double Value = value;
    public override readonly string ToString()
    {
        return $"{Value * 100}% on {PricePoint.GetEnumDescription()}";
    }
}
