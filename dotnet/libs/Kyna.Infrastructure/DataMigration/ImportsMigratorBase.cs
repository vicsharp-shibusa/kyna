using Kyna.Infrastructure.Database;

namespace Kyna.Infrastructure.DataMigration;

public abstract class ImportsMigratorBase
{
    protected Guid? _processId;
    protected readonly bool _dryRun;

    private protected readonly IDbContext _sourceContext;
    private protected readonly IDbContext _targetContext;

    protected ImportsMigratorBase(DbDef sourceDef, DbDef targetDef, Guid? processId = null, bool dryRun = false)
    {
        _processId = processId;
        _sourceContext = DbContextFactory.Create(sourceDef);
        _targetContext = DbContextFactory.Create(targetDef);
        _dryRun = dryRun;
    }

    public abstract string Source { get; }
}
