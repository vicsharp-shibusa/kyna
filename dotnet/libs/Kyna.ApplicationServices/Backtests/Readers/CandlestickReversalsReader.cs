using Kyna.Analysis.Technical.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kyna.ApplicationServices.Backtests.Readers;

internal class CandlestickReversalsReader : ChartReader
{
    public override IEnumerable<TradeSignal> Read(Chart chart)
    {
        throw new NotImplementedException();
    }
}
