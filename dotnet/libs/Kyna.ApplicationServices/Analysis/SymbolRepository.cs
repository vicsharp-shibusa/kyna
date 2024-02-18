using Kyna.Infrastructure.Database;

namespace Kyna.ApplicationServices.Analysis;

public sealed class SymbolRepository(DbDef finDef)
{
    private readonly IDbContext _dbContext = DbContextFactory.Create(finDef);

    public Task<IEnumerable<string>> GetAllAdjustedSymbolsForSourceAsync(string source)
    {
        return _dbContext.QueryAsync<string>(
            _dbContext.Sql.AdjustedEodPrices.FetchAllAdjustedSymbolsForSource, new { source });
    }
}
