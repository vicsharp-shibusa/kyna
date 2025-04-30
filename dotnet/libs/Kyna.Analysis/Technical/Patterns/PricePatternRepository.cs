using System.Reflection;

namespace Kyna.Analysis.Technical.Patterns;

internal static class PricePatternRepository
{
    private static readonly Type[] _ohlcPatternTypes;

    static PricePatternRepository()
    {
        _ohlcPatternTypes = [.. Assembly.GetExecutingAssembly()
            .GetTypes().Where(t => t.IsSubclassOf(typeof(PricePatternBase)))];
    }

    public static Type[] GetTypes() => _ohlcPatternTypes;
}
