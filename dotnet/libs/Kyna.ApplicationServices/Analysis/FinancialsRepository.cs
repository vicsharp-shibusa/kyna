using Kyna.Analysis;
using Kyna.Analysis.Technical;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;

namespace Kyna.ApplicationServices.Analysis;

public sealed class FinancialsRepository
{
    private readonly IDbContext _dbContext;
    //private readonly HashSet<InvestableEntity> _entities;

    public FinancialsRepository(DbDef finDef)
    {
        _dbContext = DbContextFactory.Create(finDef);
//        _entities = new(10_000);

//        string sql = @$"{_dbContext.Sql.Fundamentals.FetchEntity}
//WHERE source = @Source";
//        var entityDaos = _dbContext.Query<Entity>(sql, new { source });

//        foreach (var entity in entityDaos)
//        {
//            var splitSql = @$"{_dbContext.Sql.Splits.Fetch}
//WHERE source = @Source AND code = @Code";
//            var splitDaos = _dbContext.Query<Infrastructure.Database.DataAccessObjects.Split>(
//                splitSql, new { source, entity.Code });
//            var splits = splitDaos.Select(s => s.ToDomainSplit());

//            _entities.Add(new InvestableEntity(entity.Source, entity.Code)
//            {
//                Country = entity.Country,
//                Splits = splits.ToArray(),
//                Exchange = entity.Exchange,
//                GicGroup = entity.GicGroup,
//                GicIndustry = entity.GicIndustry,
//                GicSector = entity.GicSector,
//                GicSubIndustry = entity.GicSubIndustry,
//                Industry = entity.Industry,
//                Name = entity.Name,
//                Phone = entity.Phone,
//                WebUrl = entity.WebUrl,
//                Sector = entity.Sector,
//                Type = entity.Type?.GetEnumValueFromDescription<EntityType>() ?? EntityType.Unknown,
//                Currency = entity.Currency,
//                Dividends = [],
//                IsIgnored = entity.Ignored,
//                IgnoredReason = entity.IgnoredReason,
//                IsDelisted = entity.Delisted,
//            });
//        }
    }

    //public InvestableEntity[] Entities => [.. _entities];

    public Task<IEnumerable<string>> GetAllAdjustedSymbolsForSourceAsync(string source)
    {
        return _dbContext.QueryAsync<string>(
            _dbContext.Sql.AdjustedEodPrices.FetchAllAdjustedSymbolsForSource, new { source });
    }

    public async Task<IEnumerable<Ohlc>> GetOhlcForSourceAndCodeAsync(string source, string code,
        DateOnly? start = null, DateOnly? end = null,
        bool useAdjusted = true)
    {
        string sql = BuildSql(useAdjusted, start, end);

        if (useAdjusted)
        {
            var prices = (await _dbContext.QueryAsync<AdjustedEodPrice>(sql, 
                new { source, code, start, Finish = end }).ConfigureAwait(false))
                .ToArray();

            // If we don't find adjusted prices, try to find the raw prices.
            if (prices.Length == 0)
            {
                return await GetOhlcForSourceAndCodeAsync(source, code, start, end, false).ConfigureAwait(false);
            }

            return prices.Select(p => p.ToOhlc());
        }
        else
        {
            var prices = await _dbContext.QueryAsync<EodPrice>(sql, new { source, code, start, Finish = end }).ConfigureAwait(false);
            return prices.Select(p => p.ToOhlc());
        }
    }

    private string BuildSql(bool useAdjusted, DateOnly? start = null, DateOnly? end = null)
    {
        var whereClause = BuildDateRangeWhereClause(start, end);

        return useAdjusted
            ? $"{_dbContext.Sql.AdjustedEodPrices.Fetch} WHERE source = @Source AND code = @Code {whereClause}".Trim()
            : $"{_dbContext.Sql.EodPrices.Fetch} WHERE source = @Source AND code = @Code {whereClause}".Trim();
    }

    private static string BuildDateRangeWhereClause(DateOnly? start = null, DateOnly? end = null)
    {
        List<string> whereClauses = new(2);

        if (start.HasValue)
        {
            whereClauses.Add("date_eod >= @Start");
        }

        if (end.HasValue)
        {
            whereClauses.Add("date_eod <= @Finish");
        }

        var result = string.Join(" AND ", whereClauses).Trim();
        return string.IsNullOrEmpty(result) ? "" : $" AND {result}";
    }
}
