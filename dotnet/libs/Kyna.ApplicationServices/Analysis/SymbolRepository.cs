using Kyna.Infrastructure.Database;

namespace Kyna.ApplicationServices.Analysis;

public class SymbolRepository
{
    private readonly IDbContext _dbContext;

    public SymbolRepository(DbDef finDef)
    {
        _dbContext = DbContextFactory.Create(finDef);
    }

    public Task<IEnumerable<string>> GetAllAdjustedSymbolsForSourceAsync(string source)
    {
        return _dbContext.QueryAsync<string>(
            _dbContext.Sql.AdjustedEodPrices.FetchAllAdjustedSymbolsForSource, new { source });
    }
}
