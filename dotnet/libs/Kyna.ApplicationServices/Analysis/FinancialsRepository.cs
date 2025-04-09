using Kyna.Analysis.Technical.Charts;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using System.Data;

namespace Kyna.ApplicationServices.Analysis;

public sealed class FinancialsRepository
{
    private readonly DbDef _dbDef;
    private readonly IDbConnection _dbContext;

    public FinancialsRepository(DbDef finDef)
    {
        _dbDef = finDef;
        _dbContext = _dbDef.GetConnection();
    }

    public Task<IEnumerable<string>> GetAllAdjustedSymbolsForSourceAsync(string source)
    {
        return _dbContext.QueryAsync<string>(
            _dbDef.Sql.GetSql(SqlKeys.FetchAllAdjustedSymbolsForSource), new { source });
    }

    public async Task<IEnumerable<Ohlc>> GetOhlcForSourceAndCodeAsync(string source, string code,
        DateOnly? start = null, DateOnly? end = null,
        bool useAdjusted = true)
    {
        string sql = BuildSql(useAdjusted, start, end);

        if (useAdjusted)
        {
            var prices = (await _dbContext.QueryAsync<EodAdjustedPrice>(sql,
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
        List<string> whereClauses =
        [
            "source = @Source",
            "code = @Code"
        ];

        if (start.HasValue)
        {
            whereClauses.Add("date_eod >= @Start");
        }

        if (end.HasValue)
        {
            whereClauses.Add("date_eod <= @Finish");
        }


        return useAdjusted
            ? $"{_dbDef.Sql.GetSql(SqlKeys.FetchAdjustedEodPrices, [.. whereClauses])}".Trim()
            : $"{_dbDef.Sql.GetSql(SqlKeys.FetchEodPrices, [.. whereClauses])}".Trim();
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
