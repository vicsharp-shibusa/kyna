using Kyna.Analysis.Technical.Signals;
using Kyna.Backtests.AlphaModel;
using Kyna.Common;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Events;
using System.Data;
using System.Diagnostics;

namespace Kyna.ApplicationServices.Backtests.Runners;

public sealed class BacktestingService(DbDef finDef, DbDef backtestDef) : IDisposable
{
    private readonly DbDef _finDef = finDef;
    private readonly DbDef _bckDef = backtestDef;
    private readonly IDbConnection _backtestConn = backtestDef.GetConnection();
    private IBacktestRunner? _backtestRunner;

    public event EventHandler<CommunicationEventArgs>? Communicate;

    private void BacktestRunner_Communicate(object? sender, CommunicationEventArgs e)
    {
        Communicate?.Invoke(sender, e);
    }

    public Task<IEnumerable<ProcessIdInfo>> GetBacktestProcessesAsync() =>
        _backtestConn.QueryAsync<ProcessIdInfo>(
            _bckDef.GetSql(SqlKeys.FetchBacktestsProcessIdInfo));

    public async Task DeleteProcessesAsync(params Guid[] processIds)
    {
        foreach (var pid in processIds)
        {
            await _backtestConn.ExecuteAsync(_bckDef.GetSql(SqlKeys.DeleteBacktestsForProcessId),
                new { ProcessId = pid });
        }
    }

    public Task ExecuteAsync(FileInfo configFile) => ExecuteAsync([configFile]);

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

    public void Dispose()
    {
        if (_backtestRunner != null)
        {
            _backtestRunner.Communicate -= BacktestRunner_Communicate;
        }
        _backtestConn?.Dispose();
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