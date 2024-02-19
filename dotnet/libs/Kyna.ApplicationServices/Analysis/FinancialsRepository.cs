using Kyna.Analysis.Technical;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;

namespace Kyna.ApplicationServices.Analysis;

public sealed class FinancialsRepository(DbDef finDef)
{
    private readonly IDbContext _dbContext = DbContextFactory.Create(finDef);

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
            var prices = (await _dbContext.QueryAsync<AdjustedEodPrice>(sql, new { source, code, start, Finish = end })).ToArray();

            // If we don't find adjusted prices, try to find the raw prices.
            if (prices.Length == 0)
            {
                return await GetOhlcForSourceAndCodeAsync(source, code, start, end, false);
            }

            return prices.Select(p => p.ToOhlc());
        }
        else
        {
            var prices = await _dbContext.QueryAsync<EodPrice>(sql, new { source, code, start, Finish = end });
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
