//using Kyna.Common;
//using Kyna.Common.Events;
//using Kyna.Infrastructure.Database;
//using Kyna.Infrastructure.Database.DataAccessObjects;
//using Kyna.Infrastructure.DataImport;
//using System.Diagnostics;
//using System.Text;
//using System.Text.Json;

//namespace Kyna.Infrastructure.DataMigration;

//public sealed class ImportsMigrator(DbDef sourceDef, DbDef targetDef,
//    MigrationConfiguration configuration, Guid? processId = null, bool dryRun = false)
//{
//    private readonly Guid? _processId = processId;
//    private readonly bool _dryRun = dryRun;

//    private readonly IDbContext _sourceContext = DbContextFactory.Create(sourceDef);
//    private readonly IDbContext _targetContext = DbContextFactory.Create(targetDef);

//    private readonly MigrationConfiguration _configuration = configuration;

//    private readonly List<int> _itemIdsToDelete = [];

//    public event EventHandler<CommunicationEventArgs>? Communicate;

//    public TimeSpan Migrate()
//    {
//        Stopwatch timer = Stopwatch.StartNew();

//        var itemsArray = GetTransactionsToMigrate().ToArray();

//        _itemIdsToDelete.Capacity = itemsArray.Length;

//        int itemsMigratedCount = MigrateByMode(itemsArray);

//        DeleteApiTransactionsFromSource();

//        int adjItemsCount = MigrateEodPricesToEodAdjustedPrices();

//        MigrateNonSplitPricesToAdjustedPrices();

//        DeleteEodPrices();

//        Communicate?.Invoke(this, new CommunicationEventArgs($"Items Migrated from api transactions  : {itemsMigratedCount:#,##0}", null));
//        Communicate?.Invoke(this, new CommunicationEventArgs($"Items Migrated to EOD adjusted prices : {adjItemsCount:#,##0}", null));

//        timer.Stop();
//        return timer.Elapsed;
//    }

//    private int MigrateByMode(ApiTransactionForMigration[] items) => _configuration.Mode switch
//    {
//        MigrationSourceMode.Latest => MigrateLatest(items),
//        MigrationSourceMode.Rolling => MigrateRolling(items),
//        _ => throw new ArgumentException($"Migration mode of {_configuration.Mode.GetEnumDescription()} was provided; no migration occurred."),
//    };

//    private void DeleteApiTransactionsFromSource()
//    {
//        const int ChunkSize = 500;

//        if (_itemIdsToDelete.Count > 0)
//        {
//            Communicate?.Invoke(this, new CommunicationEventArgs($"Starting deletion process; {_itemIdsToDelete.Count} records to delete", null));

//            foreach (var ids in _itemIdsToDelete.Chunk(ChunkSize))
//            {
//                if (_dryRun)
//                {
//                    foreach (var id in ids)
//                    {
//                        Communicate?.Invoke(this, new CommunicationEventArgs($"deleting {id}", null));
//                    }
//                }
//                else
//                {
//                    Communicate?.Invoke(this, new CommunicationEventArgs($"deleting {ids.Length} records", null));
//                    _sourceContext.Execute(
//                        $"{_sourceContext.Sql.ApiTransactions.Delete} WHERE id {_sourceContext.Sql.GetInCollectionSql("ids")}", 
//                        ids);
//                }
//            }
//        }
//        else
//        {
//            Communicate?.Invoke(this, new CommunicationEventArgs("Nothing to delete.", null));
//        }
//    }

//    private int MigrateLatest(ApiTransactionForMigration[] itemsArray)
//    {
//        int itemsMigrated = 0;

//        var itemsToMigrate = itemsArray.GroupBy(g => new { g.Category, g.SubCategory })
//            .Select(g => new
//            {
//                g.Key,
//                Item = g.MaxBy(i => i.Id)
//            }).Where(i => i?.Item != null &&
//                (_configuration.Categories.Length == 0 || _configuration.Categories.Contains(i.Key.Category))).ToArray();

//        if (_configuration.MaxParallelization > 1)
//        {
//            CancellationTokenSource cts = new();
//            Parallel.ForEach(itemsToMigrate.Select(i => i.Item),
//                new ParallelOptions()
//                {
//                    MaxDegreeOfParallelism = _configuration.MaxParallelization,
//                    CancellationToken = cts.Token
//                },
//                (item) =>
//                {
//                    itemsMigrated += ProcessApiTransactions(item);
//                });
//            cts.Dispose();
//        }
//        else
//        {
//            foreach (var item in itemsToMigrate)
//            {
//                itemsMigrated += ProcessApiTransactions(item.Item);
//            }
//        }

//        if (_configuration.SourceDeletionMode == SourceDeletionMode.All)
//        {
//            _itemIdsToDelete.AddRange(itemsToMigrate.Select(i => i.Item!.Id));
//        }

//        if (_configuration.SourceDeletionMode == SourceDeletionMode.AllExceptLatest)
//        {
//            foreach (var item in itemsToMigrate)
//            {
//                _itemIdsToDelete.AddRange(itemsArray.Select(i => i.Id)
//                    .Except(itemsToMigrate.Select(i => i.Item!.Id)));
//            }
//        }

//        return itemsMigrated;
//    }

//    private int MigrateRolling(ApiTransactionForMigration[] itemsArray)
//    {
//        int itemsMigrated = 0;

//        var itemsToMigrate = itemsArray.GroupBy(g => new { g.Category, g.SubCategory })
//            .Select(g => new
//            {
//                g.Key,
//                Items = g.OrderBy(i => i.Id).ToArray()
//            }).Where(i => i?.Items != null &&
//                (_configuration.Categories.Length == 0 || _configuration.Categories.Contains(i.Key.Category))).ToArray();

//        if (_configuration.MaxParallelization > 1)
//        {
//            CancellationTokenSource cts = new();
//            Parallel.ForEach(itemsToMigrate.SelectMany(i => i.Items),
//                new ParallelOptions()
//                {
//                    MaxDegreeOfParallelism = _configuration.MaxParallelization,
//                    CancellationToken = cts.Token
//                },
//                (item) =>
//                {
//                    itemsMigrated += ProcessApiTransactions(item);
//                });
//            cts.Dispose();
//        }
//        else
//        {
//            foreach (var item in itemsToMigrate.SelectMany(i => i.Items))
//            {
//                itemsMigrated += ProcessApiTransactions(item);
//            }
//        }

//        if (_configuration.SourceDeletionMode == SourceDeletionMode.All)
//        {
//            _itemIdsToDelete.AddRange(itemsToMigrate.SelectMany(i => i.Items).Select(i => i.Id));
//        }

//        if (_configuration.SourceDeletionMode == SourceDeletionMode.AllExceptLatest)
//        {
//            foreach (var item in itemsToMigrate)
//            {
//                _itemIdsToDelete.AddRange(item.Items.Select(i => i.Id)
//                    .Except(new int[] { item.Items.Last().Id }));
//            }
//        }

//        return itemsMigrated;
//    }

//    private IEnumerable<ApiTransactionForMigration> GetTransactionsToMigrate()
//    {
//        return _sourceContext.Query<ApiTransactionForMigration>(BuildFetchForMigrationSql(),
//            new { _configuration.Source, _configuration.Categories });
//    }

//    private string BuildFetchForMigrationSql()
//    {
//        StringBuilder sb = new(_sourceContext.Sql.ApiTransactions.FetchForMigration);
//        sb.AppendLine();
//        sb.AppendLine("WHERE source = @Source");
//        if (_configuration.Categories.Length > 0)
//        {
//            sb.AppendLine($"AND category {_sourceContext.Sql.GetInCollectionSql("Categories")}");
//        }
//        sb.AppendLine($"AND response_status_code = '200'");
//        return sb.ToString();
//    }

//    private int ProcessApiTransactions(ApiTransactionForMigration? item)
//    {
//        int itemsMigrated = 0;
//        if (item == null)
//        {
//            return itemsMigrated;
//        }

//        Communicate?.Invoke(this, new CommunicationEventArgs($"migrating {item.Category} {item.SubCategory}", null));
//        if (_dryRun)
//        {
//            return itemsMigrated;
//        }

//        if (item.ResponseStatusCode != "200")
//        {
//            _itemIdsToDelete.Add(item.Id);
//            Communicate?.Invoke(this, new CommunicationEventArgs($"Bad record: {item.Id}", null));
//            return itemsMigrated;
//        }

//        var responseBody = _sourceContext.QueryFirstOrDefault<string>(
//            _sourceContext.Sql.ApiTransactions.FetchResponseBodyForId,
//            new { item.Id });

//        if (!string.IsNullOrWhiteSpace(responseBody))
//        {
//            // TODO: we should use the constants that correspond to our source.
//            if (item.Category.Equals(EodHdImporter.Constants.Actions.EndOfDayPrices))
//            {
//                itemsMigrated += MigrateEodPrices(item, responseBody);
//            }

//            if (item.Category.Equals(EodHdImporter.Constants.Actions.Splits))
//            {
//                itemsMigrated += MigrateSplits(item, responseBody);
//            }

//            // TODO: Need to handle other categories.
//        }

//        return itemsMigrated;
//    }

//    private int MigrateEodPrices(ApiTransactionForMigration item, string responseBody)
//    {
//        // TODO: Need to refactor to consider other sources.
//        int itemsMigrated = 0;
//        var eodPriceActions = JsonSerializer.Deserialize<EodHistoricalData.PriceAction[]>(
//            responseBody, JsonOptionsRepository.DefaultSerializerOptions);

//        if ((eodPriceActions?.Length ?? 0) > 0)
//        {
//            itemsMigrated++;
//            _targetContext.Execute(_sourceContext.Sql.EodPrices.Upsert,
//                eodPriceActions!.Select(p => new EodPrice(item.Source,
//                item.SubCategory,
//                p.Date, p.Open, p.High, p.Low, p.Close, p.Volume,
//                DateTime.UtcNow.Ticks,
//                DateTime.UtcNow.Ticks,
//                _processId)));
//        }
//        return itemsMigrated;
//    }

//    private int MigrateSplits(ApiTransactionForMigration item, string responseBody)
//    {
//        // TODO: Need to refactor to consider other sources.
//        int itemsMigrated = 0;
//        var splits = JsonSerializer.Deserialize<EodHistoricalData.Split[]>(
//            responseBody, JsonOptionsRepository.DefaultSerializerOptions);

//        if ((splits?.Length ?? 0) > 0)
//        {
//            itemsMigrated++;
//            _targetContext.Execute(_sourceContext.Sql.Splits.Upsert,
//                splits!.Select(s => new Split(item.Source, item.SubCategory,
//                s.Date, s.SplitText, _processId)));
//        }
//        return itemsMigrated;
//    }

//    private int MigrateEodPricesToEodAdjustedPrices()
//    {
//        int itemsMigrated = 0;

//        Communicate?.Invoke(this, new CommunicationEventArgs("Fetching splits", null));
//        var splits = _targetContext.Query<Split>(_targetContext.Sql.Splits.Fetch)
//            .GroupBy(g => new { g.Source, g.Code })
//            .Select(g => new
//            {
//                g.Key,
//                Items = g.OrderBy(i => i.SplitDate).ToArray()
//            }).Where(i => i?.Items != null).ToArray();

//        if (splits.Length > 0)
//        {
//            string eodSql = $"{_targetContext.Sql.EodPrices.Fetch} WHERE source = @Source AND code = @Code";
//            CancellationTokenSource cts = new();

//            if (_configuration.MaxParallelization > 1)
//            {
//                Parallel.ForEach(splits,
//                    new ParallelOptions()
//                    {
//                        MaxDegreeOfParallelism = _configuration.MaxParallelization,
//                        CancellationToken = cts.Token
//                    },
//                    (split) =>
//                    {
//                        var chart = _targetContext.Query<EodPrice>(eodSql, new { split.Key.Source, split.Key.Code }).ToArray();
//                        if (chart.Length > 0)
//                        {
//                            var adjustedChart = SplitAdjustedPriceCalculator.Calculate(chart, split.Items);

//                            Communicate?.Invoke(this, new CommunicationEventArgs($"Migrating {split.Key.Code} to adjusted prices table", null));

//                            _targetContext.Execute(_targetContext.Sql.AdjustedEodPrices.Upsert, adjustedChart);
//                        }
//                    });
//            }
//            else
//            {
//                foreach (var split in splits)
//                {
//                    var chart = _targetContext.Query<EodPrice>(eodSql, new { split.Key.Source, split.Key.Code }).ToArray();
//                    if (chart.Length > 0)
//                    {
//                        var adjustedChart = SplitAdjustedPriceCalculator.Calculate(chart, split.Items);

//                        Communicate?.Invoke(this, new CommunicationEventArgs($"Migrating {split.Key.Code} to adjusted prices table", null));

//                        _targetContext.Execute(_targetContext.Sql.AdjustedEodPrices.Upsert, adjustedChart);
//                    }
//                }
//            }

//            cts.Dispose();
//        }

//        return itemsMigrated;
//    }

//    private void MigrateNonSplitPricesToAdjustedPrices()
//    {
//        // If we're migrating all prices to the adjusted prices, 
//        // migrate all the data that has no splits.
//        // This has a commandTimeout of 0 because it will take several minutes to complete.
//        if (_configuration.AdjustedPriceModes.HasFlag(AdjustedPriceModes.All))
//        {
//            Communicate?.Invoke(this, new CommunicationEventArgs("Migrating non-split data to adjusted prices table ...", null));
//            Communicate?.Invoke(this, new CommunicationEventArgs("This may take several minutes.", null));

//            _targetContext.Execute(
//                _targetContext.Sql.AdjustedEodPrices.MigratePricesWithoutSplitsToAdjustedPrices,
//                commandTimeout: 0);
//        }
//    }

//    private void DeleteEodPrices()
//    {
//        Communicate?.Invoke(this, new CommunicationEventArgs("Deleting EOD prices", null));

//        if (_configuration.AdjustedPriceModes.HasFlag(AdjustedPriceModes.DeleteFromSource))
//        {
//            Communicate?.Invoke(this, new CommunicationEventArgs("Deleting from EOD prices where adjusted prices exist ...", null));
//            Communicate?.Invoke(this, new CommunicationEventArgs("This may take several minutes.", null));

//            _targetContext.Execute(_targetContext.Sql.EodPrices.DeleteEodPricesWithAdjustedPrices, commandTimeout: 0);
//        }
//    }
//}
