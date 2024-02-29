using Kyna.Analysis.Technical;

namespace Kyna.Backtests;

public struct TestTargetPercentage
{

    public PricePoint PricePoint;
    public double Value;

    public TestTargetPercentage(PricePoint pricePoint, double value)
    {
        PricePoint = pricePoint;
        Value = value;
    }
}
