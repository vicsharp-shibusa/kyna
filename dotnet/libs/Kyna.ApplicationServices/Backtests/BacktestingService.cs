using Kyna.Analysis.Technical;
using Kyna.Analysis.Technical.Signals;
using Kyna.ApplicationServices.Backtests.Runners;
using Kyna.Backtests;
using Kyna.Common;
using Kyna.Common.Events;
using Kyna.Infrastructure.Database;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Kyna.ApplicationServices.Backtests;

public sealed class BacktestingService : IDisposable
{
    private readonly IBacktestRunner? _backtestRunner;
    private bool _disposedValue;
    private readonly BacktestingConfiguration? _configuration;

    public event EventHandler<CommunicationEventArgs>? Communicate;

    public BacktestingService(DbDef finDef, DbDef backtestDef, 
        FileInfo? configFileInfo, 
        Guid? processId = null)
    {
        if (!(configFileInfo?.Exists ?? false))
        {
            throw new ArgumentException($"Confile file '{configFileInfo?.Name ?? ""}' does not exist.");
        }

        var options = JsonOptionsRepository.DefaultSerializerOptions;
        options.Converters.Add(new EnumDescriptionConverter<BacktestType>());
        options.Converters.Add(new EnumDescriptionConverter<PricePoint>());

        _configuration = JsonSerializer.Deserialize<BacktestingConfiguration>(
            File.ReadAllText(configFileInfo.FullName),
            JsonOptionsRepository.DefaultSerializerOptions) ?? throw new ArgumentException($"Could not deserialize {configFileInfo.Name}");

        _backtestRunner = BacktestRunnerFactory.Create(finDef, backtestDef, _configuration,
            processId);
        if (_backtestRunner == null)
        {
            throw new ArgumentException($"Could not construct backtest runner for type '{_configuration?.Type.GetEnumDescription()}'");
        }
        _backtestRunner.Communicate += BacktestRunner_Communicate;
    }

    private void BacktestRunner_Communicate(object? sender, CommunicationEventArgs e)
    {
        Communicate?.Invoke(sender, e);
    }

    public void WriteConfigInfo()
    {
        if (_configuration != null)
        {
            StringBuilder sb = new();

            sb.AppendLine($"Type               : {_configuration.Type.GetEnumDescription()}");
            sb.AppendLine($"Name               : {_configuration.Name}");
            sb.AppendLine($"Source             : {_configuration.Source}");
            sb.AppendLine($"Description        : {_configuration.Description}");
            sb.AppendLine($"Entry Price Point  : {_configuration.EntryPricePoint.GetEnumDescription()}");
            sb.AppendLine($"Target Up          : {_configuration.TargetUp}");
            sb.AppendLine($"Target Down        : {_configuration.TargetDown}");
            sb.AppendLine($"Length of Prologue : {_configuration.LengthOfPrologue}");
            if ((_configuration.SignalNames?.Length ?? 0) > 0)
            {
                sb.AppendLine("Signal Names:");
                foreach (var sn in _configuration.SignalNames!)
                {
                    sb.AppendLine($"\t{sn}");
                }
            }

            sb.AppendLine();

            Communicate?.Invoke(this, new CommunicationEventArgs(sb.ToString(), nameof(BacktestingService)));
        }
    }

    public void WriteActivityCounts() => _backtestRunner?.WriteActivityCounts();

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
    public static IBacktestRunner? Create(DbDef finDef, DbDef backtestDef, 
        BacktestingConfiguration configuration,
        Guid? processId = null)
    {
        if (configuration.Type == BacktestType.CandlestickPattern)
        {
            if ((configuration.SignalNames?.Length ?? 0) == 0)
            {
                throw new ArgumentException($"Backtest configuration must contain at least one signal when the type is {configuration.Type.GetEnumDescription()}");
            }
            var repo = new CandlestickSignalRepository(new SignalOptions(configuration.LengthOfPrologue));
            CandlestickSignal[] signals = new CandlestickSignal[configuration.SignalNames!.Length];
            for (int c = 0; c < configuration.SignalNames.Length; c++)
            {
                var signal = repo.Find(configuration.SignalNames[c]) ?? throw new Exception($"Could not find signal for '{configuration.SignalNames[c]}'");
                signals[c] = signal;
            }
            return new CandlestickSignalRunner(finDef, backtestDef, configuration,
                signals, processId);
        }
        if (configuration.Type == BacktestType.RandomBaseline)
        {
            return new RandomBaselineRunner(finDef, backtestDef, configuration, processId);
        }
        return null;
    }
}