using Kyna.Analysis.Technical;
using Kyna.ApplicationServices.Backtests.Runners;
using Kyna.Backtests;
using Kyna.Common;
using Kyna.Common.Events;
using Kyna.Infrastructure.Database;
using System.Diagnostics;
using System.Text.Json;

namespace Kyna.ApplicationServices.Backtests;

public sealed class BacktestingService : IDisposable
{
    private readonly IBacktestRunner? _backtestRunner;
    private bool _disposedValue;

    public event EventHandler<CommunicationEventArgs>? Communicate;

    public BacktestingService(DbDef finDef, DbDef backtestDef, FileInfo? configFileInfo, Guid? processId = null)
    {
        if (!(configFileInfo?.Exists ?? false))
        {
            throw new ArgumentException($"Confile file '{configFileInfo?.Name ?? ""}' does not exist.");
        }

        var options = JsonOptionsRepository.DefaultSerializerOptions;
        options.Converters.Add(new EnumDescriptionConverter<BacktestType>());
        options.Converters.Add(new EnumDescriptionConverter<PricePoint>());

        var configuration = JsonSerializer.Deserialize<BacktestingConfiguration>(
            File.ReadAllText(configFileInfo.FullName),
            JsonOptionsRepository.DefaultSerializerOptions) ?? throw new ArgumentException($"Could not deserialize {configFileInfo.Name}");

        _backtestRunner = BacktestRunnerFactory.Create(finDef, backtestDef, configuration, processId);
        if (_backtestRunner == null)
        {
            throw new ArgumentException($"Could not construct backtest runner for type '{configuration?.Type.GetEnumDescription()}'");
        }
        _backtestRunner.Communicate += BacktestRunner_Communicate;
    }

    private void BacktestRunner_Communicate(object? sender, CommunicationEventArgs e)
    {
        Communicate?.Invoke(sender, e);
    }

    public Task ExecuteAsync()
    {
        CancellationTokenSource cts = new();

        Debug.Assert(_backtestRunner != null);

        return _backtestRunner!.ExecuteAsync(cts.Token);
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
    public static IBacktestRunner? Create(DbDef finDef, DbDef backtestDef, BacktestingConfiguration configuration,
        Guid? processId = null)
    {
        if (configuration.Type == BacktestType.RandomBaseline)
        {
            return new RandomBaselineRunner(finDef, backtestDef, configuration, processId);
        }
        return null;
    }
}