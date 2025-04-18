using Kyna.Analysis.Technical.Charts;

namespace Kyna.ApplicationServices.Backtests.Readers;

internal abstract class OhlcReader
{
    public abstract IEnumerable<TradeSignal> Read(Ohlc[] priceData);
}
