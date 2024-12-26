using Kyna.Analysis.Technical;
using Kyna.Common;

namespace Kyna.Backtests.AlphaModel;

public class BacktestResult(BacktestingConfiguration configuration)
{
    private readonly List<BacktestResultDetail> _results = new(10_000);
    public Guid Id { get; init; } = Guid.NewGuid();
    public BacktestingConfiguration Configuration { get; init; } = configuration;
    public BacktestResultDetail[] Details => [.. _results];
    public void AddDetail(BacktestResultDetail detail)
    {
        _results.Add(detail);
    }
}

public struct ResultDetail
{
    public DateOnly? Date;
    public PricePoint PricePoint;
    public decimal Price;
}

public class BacktestResultDetail
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid BacktestId { get; init; }
    public required string SignalName { get; init; }
    public required string Code { get; init; }
    public string? Industry { get; init; }
    public string? Sector { get; init; }
    public ResultDetail Entry { get; init; }
    public ResultDetail? Up { get; init; }
    public ResultDetail? Down { get; init; }
    public ResultDetail? Winner
    {
        get
        {
            if (Up.GetValueOrDefault().Date.HasValue && Down.GetValueOrDefault().Date.HasValue)
            {
                if (Up.GetValueOrDefault().Date <= Down.GetValueOrDefault().Date)
                {
                    return Up;
                }
                return Down;
            }
            return Up.GetValueOrDefault().Date.HasValue ? Up
                : Down.GetValueOrDefault().Date.HasValue ? Down
                : null;
        }
    }

    public string? WinnerText => Winner == null ? null
        : Winner.Equals(Up) ? nameof(Up)
        : nameof(Down);

    public int? WinnerDurationTradingDays
    {
        get
        {
            var winner = Winner;
            if (winner == null)
            {
                return null;
            }
            return Entry.Date.GetValueOrDefault()
                .CountWeekdays(winner.GetValueOrDefault().Date.GetValueOrDefault(), false);
        }
    }

    public int? WinnerDurationCalendarDays
    {
        get
        {
            var winner = Winner;
            if (winner == null)
            {
                return null;
            }
            return winner!.GetValueOrDefault().Date.GetValueOrDefault().DayNumber -
                 Entry.Date.GetValueOrDefault().DayNumber;
        }
    }
}
