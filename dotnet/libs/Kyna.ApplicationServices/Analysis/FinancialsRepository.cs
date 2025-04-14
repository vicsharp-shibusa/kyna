using Kyna.Analysis.Technical.Charts;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using System.Data;

namespace Kyna.ApplicationServices.Analysis;

public sealed class FinancialsRepository
{
    private readonly DbDef _dbDef;

    public FinancialsRepository(DbDef finDef)
    {
        ArgumentNullException.ThrowIfNull(finDef);
        _dbDef = finDef;
    }

    public Task<IEnumerable<string>> GetAllAdjustedSymbolsForSourceAsync(string source)
    {
        using var conn = _dbDef.GetConnection();
        return conn.QueryAsync<string>(
            _dbDef.Sql.GetSql(SqlKeys.FetchAllAdjustedSymbolsForSource), new { source });
    }

    public async Task<IEnumerable<Ohlc>> GetOhlcForSourceAndCodeAsync(string source, string code,
        DateOnly? start = null, DateOnly? end = null, bool useAdjusted = true)
    {
        string sql = BuildSql(useAdjusted, start, end);

        if (useAdjusted)
        {
            using var conn = _dbDef.GetConnection();
            var prices = (await conn.QueryAsync<EodAdjustedPrice>(sql,
                new { source, code, start, Finish = end }).ConfigureAwait(false))
                .ToArray();
            conn.Close();

            // If we don't find adjusted prices, try to find the raw prices.
            if (prices.Length == 0)
            {
                return await GetOhlcForSourceAndCodeAsync(source, code, start, end, false).ConfigureAwait(false);
            }

            return prices.Select(p => p.ToOhlc());
        }
        else
        {
            using var conn = _dbDef.GetConnection();
            var prices = await conn.QueryAsync<EodPrice>(sql, new { source, code, start, Finish = end }).ConfigureAwait(false);
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
            whereClauses.Add("date_eod >= @Start");

        if (end.HasValue)
            whereClauses.Add("date_eod <= @Finish");

        return useAdjusted
            ? $"{_dbDef.Sql.GetSql(SqlKeys.FetchEodAdjustedPrices, [.. whereClauses])}".Trim()
            : $"{_dbDef.Sql.GetSql(SqlKeys.FetchEodPrices, [.. whereClauses])}".Trim();
    }
}
