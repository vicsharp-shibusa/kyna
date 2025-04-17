using Kyna.Analysis.Technical;
using Kyna.Backtests;
using Kyna.Backtests.AlphaModel;
using Kyna.Common;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Kyna.Infrastructure.Events;
using System.Collections.Concurrent;
using System.Data;
using System.Text;
using System.Text.Json;

namespace Kyna.ApplicationServices.Backtests.Runners;

internal abstract class RunnerBase
{
    protected readonly IDbConnection _finDbContext;
    protected readonly IDbConnection _backtestDbContext;
    protected readonly DbDef _finDbDef;
    protected readonly DbDef _backtestDbDef;
    protected readonly ActivityCounts _activityCounts;
    protected readonly Guid _processId = Guid.NewGuid();
    private readonly ConcurrentQueue<BacktestResultDetail> _resultDetails;
    private readonly bool _runQueue = true;

    public event EventHandler<CommunicationEventArgs>? Communicate;

    public RunnerBase(DbDef finDef, DbDef backtestsDef)
    {
        _finDbDef = finDef;
        _backtestDbDef = backtestsDef;
        _finDbContext = finDef.GetConnection();
        _backtestDbContext = backtestsDef.GetConnection();
        _resultDetails = new();
        _activityCounts = new();
        RunResultDetailDequeue();
    }

    protected void WriteConfigInfo(FileInfo configFile)
    {
        if (Communicate != null)
        {
            var configuration = DeserializeConfigFile(configFile);

            if (configuration != null)
            {
                StringBuilder sb = new();

                sb.AppendLine($"Type               : {configuration.Type.GetEnumDescription()}");
                sb.AppendLine($"Name               : {configuration.Name}");
                sb.AppendLine($"Source             : {configuration.Source}");
                sb.AppendLine($"Description        : {configuration.Description}");
                sb.AppendLine($"Entry Price Point  : {configuration.EntryPricePoint.GetEnumDescription()}");
                sb.AppendLine($"Target Up          : {configuration.TargetUp}");
                sb.AppendLine($"Target Down        : {configuration.TargetDown}");
                sb.AppendLine($"Length of Lookback : {configuration.LookbackLength}");
                if ((configuration.SignalNames?.Length ?? 0) > 0)
                {
                    sb.AppendLine("Signal Names:");
                    foreach (var sn in configuration.SignalNames!)
                    {
                        sb.AppendLine($"\t{sn}");
                    }
                }

                sb.AppendLine();

                Communicate.Invoke(this, new CommunicationEventArgs(sb.ToString(), nameof(BacktestingService)));
            }
        }
    }

    internal static BacktestingConfiguration DeserializeConfigFile(FileInfo configFile)
    {
        if (!configFile.Exists)
        {
            throw new ArgumentException($"Configuration file, {configFile.FullName}, does not exist.");
        }
        var options = JsonSerializerOptionsRepository.Custom;
        options.Converters.Add(new EnumDescriptionConverter<BacktestType>());
        options.Converters.Add(new EnumDescriptionConverter<PricePoint>());

        return JsonSerializer.Deserialize<BacktestingConfiguration>(
            File.ReadAllText(configFile.FullName),
            options) ?? throw new ArgumentException($"Could not deserialize {configFile.FullName}");
    }

    protected virtual Task<IEnumerable<CodesAndCounts>> GetCodesAndCount(string source,
        CancellationToken cancellationToken)
    {
        OnCommunicate(new CommunicationEventArgs("Fetching data to backtest ...", null));
        return _finDbContext.QueryAsync<CodesAndCounts>(
            _finDbDef.Sql.GetSql(SqlKeys.SelectAdjustedCodesAndCounts), new { source },
            commandTimeout: 0, cancellationToken: cancellationToken);
    }

    protected virtual async Task<Guid> CreateBacktestingRecord(BacktestingConfiguration configuration,
        CancellationToken cancellationToken)
    {
        Guid backtestId = Guid.NewGuid();
        OnCommunicate(new CommunicationEventArgs("Creating backtest record...", null));
        await _backtestDbContext.ExecuteAsync(_backtestDbDef.Sql.GetSql(SqlKeys.UpsertBacktest),
            new Backtest(backtestId,
                configuration.Name,
                configuration.Type.GetEnumDescription(),
                configuration.Source,
                configuration.Description,
                configuration.EntryPricePoint.GetEnumDescription(),
                configuration.TargetUp.Value,
                configuration.TargetUp.PricePoint.GetEnumDescription(),
                configuration.TargetDown.Value,
                configuration.TargetDown.PricePoint.GetEnumDescription(),
                _processId), cancellationToken: cancellationToken);
        return backtestId;
    }

    protected virtual void Enqueue(BacktestResultDetail detail)
    {
        _resultDetails.Enqueue(detail);
    }

    protected virtual async Task WaitForQueueAsync(int milliseconds = 1_000)
    {
        while (!_resultDetails.IsEmpty)
        {
            await Task.Delay(milliseconds).ConfigureAwait(false);
        }
    }

    protected virtual void OnCommunicate(CommunicationEventArgs e)
    {
        Communicate?.Invoke(this, e);
    }

    private void RunResultDetailDequeue()
    {
        Task.Run(() =>
        {
            while (_runQueue || !_resultDetails.IsEmpty)
            {
                if (_resultDetails.TryDequeue(out BacktestResultDetail? resultDetail))
                {
                    if (resultDetail != null)
                    {
                        Communicate?.Invoke(this, new CommunicationEventArgs(
                            $"{resultDetail.SignalName}\t{resultDetail.Code}\t{resultDetail.Entry.Date:yyyy-MM-dd}", null));
                        try
                        {
                            _backtestDbContext.Execute(_backtestDbDef.Sql.GetSql(SqlKeys.UpsertBacktestResult),
                                new Infrastructure.Database.DataAccessObjects.BacktestResult(
                                    resultDetail.Id,
                                    resultDetail.BacktestId,
                                    resultDetail.SignalName,
                                    resultDetail.Code,
                                    resultDetail.Industry,
                                    resultDetail.Sector,
                                    resultDetail.Entry.Date.GetValueOrDefault(),
                                    resultDetail.Entry.PricePoint.GetEnumDescription(),
                                    resultDetail.Entry.Price,
                                    resultDetail.Up?.Date,
                                    resultDetail.Up?.PricePoint.GetEnumDescription(),
                                    resultDetail.Up?.Price,
                                    resultDetail.Down?.Date,
                                    resultDetail.Down?.PricePoint.GetEnumDescription(),
                                    resultDetail.Down?.Price,
                                    resultDetail.WinnerText,
                                    resultDetail.WinnerDurationTradingDays,
                                    resultDetail.WinnerDurationCalendarDays));

                            lock (_activityCounts)
                            {
                                _activityCounts.EventCount++;
                            }
                        }
                        catch (Exception exc)
                        {
                            Communicate?.Invoke(this, new CommunicationEventArgs(exc.ToString(), null));
                        }
                    }
                }
            }
        });
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
    protected struct CodesAndCounts
    {
        public string Code;
        public string? Industry;
        public string? Sector;
        public int Count;
    }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null

    protected class ActivityCounts
    {
        public int EventCount { get; set; }
        public int EntityCount { get; set; }
    }
}