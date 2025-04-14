namespace Kyna.Analysis.Technical.Trends;

public interface ITrend
{
    string Name { get; }
    double[] TrendValues { get; }
    void Calculate();
}
