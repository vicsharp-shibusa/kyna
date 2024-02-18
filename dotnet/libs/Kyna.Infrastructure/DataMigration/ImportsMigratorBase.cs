using Kyna.Infrastructure.Database;

namespace Kyna.Infrastructure.DataMigration;

internal abstract class ImportsMigratorBase(DbDef sourceDef, DbDef targetDef,
    Guid? processId = null, bool dryRun = false)
{
    protected Guid? _processId = processId;
    protected readonly bool _dryRun = dryRun;

    private protected readonly IDbContext _sourceContext = DbContextFactory.Create(sourceDef);
    private protected readonly IDbContext _targetContext = DbContextFactory.Create(targetDef);

    public abstract string Source { get; }
}
