using Kyna.Analysis.Technical.Signals;
using Kyna.ApplicationServices.Backtests.Runners;
using Kyna.Backtests;
using Kyna.Common;
using Kyna.Common.Events;
using Kyna.Infrastructure.Database;
using System.Diagnostics;
using static Kyna.ApplicationServices.Reports.ReportService;

namespace Kyna.ApplicationServices.Backtests;

public sealed class BacktestingService(DbDef finDef, DbDef backtestDef) : IDisposable
{
    private readonly IDbContext _backtestsCtx = DbContextFactory.Create(backtestDef);
    private IBacktestRunner? _backtestRunner;
    private bool _disposedValue;
    private DbDef _finDef = finDef, _bckDef = backtestDef;

    public event EventHandler<CommunicationEventArgs>? Communicate;

    private void BacktestRunner_Communicate(object? sender, CommunicationEventArgs e)
    {
        Communicate?.Invoke(sender, e);
    }

    public Task<IEnumerable<ProcessIdInfo>> GetBacktestProcessesAsync() =>
        _backtestsCtx.QueryAsync<ProcessIdInfo>(_backtestsCtx.Sql.Backtests.FetchProcessIdInfo);

    public async Task DeleteProcessesAsync(params Guid[] processIds)
    {
        foreach (var pid in processIds)
        {
            await _backtestsCtx.ExecuteAsync(_backtestsCtx.Sql.Backtests.DeleteForProcessId,
                new { ProcessId = pid });
        }
    }

    public Task ExecuteAsync(FileInfo configFile) =>
        ExecuteAsync([configFile]);

    public Task ExecuteAsync(FileInfo[] configFiles)
    {
        if (configFiles.Length == 0)
        {
            return Task.CompletedTask;
        }

        CancellationTokenSource cts = new();

        _backtestRunner = BacktestRunnerFactory.Create(_finDef, _bckDef, configFiles[0]);
        Debug.Assert(_backtestRunner != null);
        _backtestRunner.Communicate += BacktestRunner_Communicate;

        return _backtestRunner!.ExecuteAsync(configFiles, cts.Token);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                if (_backtestRunner != null)
                {
                    _backtestRunner.Communicate -= BacktestRunner_Communicate;
                }
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

internal static class BacktestRunnerFactory
{
    public static IBacktestRunner? Create(DbDef finDef, DbDef backtestDef,
        FileInfo configFile)
    {
        var configuration = RunnerBase.DeserializeConfigFile(configFile);
        if (configuration.Type == BacktestType.CandlestickPattern)
        {
            if ((configuration.SignalNames?.Length ?? 0) == 0)
            {
                throw new ArgumentException($"Backtest configuration must contain at least one signal when the type is {configuration.Type.GetEnumDescription()}");
            }
            var repo = new CandlestickSignalRepository(new SignalOptions(configuration.ChartConfiguration?.LengthOfPrologue ?? 15));
            CandlestickSignal[] signals = new CandlestickSignal[configuration.SignalNames!.Length];
            for (int c = 0; c < configuration.SignalNames.Length; c++)
            {
                var signal = repo.Find(configuration.SignalNames[c]) ?? throw new Exception($"Could not find signal for '{configuration.SignalNames[c]}'");
                signals[c] = signal;
            }
            return new CandlestickSignalRunner(finDef, backtestDef, configuration.Source,
                signals);
        }
        if (configuration.Type == BacktestType.RandomBaseline)
        {
            return new RandomBaselineRunner(finDef, backtestDef, configuration.Source);
        }
        return null;
    }
}