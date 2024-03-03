using Kyna.Analysis.Technical;

namespace Kyna.Backtests;

public struct TestTargetPercentage(PricePoint pricePoint, double value)
{
    public PricePoint PricePoint = pricePoint;
    public double Value = value;
}
