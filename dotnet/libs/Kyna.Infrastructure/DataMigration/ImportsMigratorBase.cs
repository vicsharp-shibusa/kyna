using Kyna.Infrastructure.Database;
using System.Data;

namespace Kyna.Infrastructure.DataMigration;

internal abstract class ImportsMigratorBase
{
    protected Guid? _processId;
    protected readonly bool _dryRun;

    private protected readonly IDbConnection _sourceContext;
    private protected readonly IDbConnection _targetContext;
    private protected readonly DbDef _sourceDbDef;
    private protected readonly DbDef _targetDbDef;

    public ImportsMigratorBase(DbDef sourceDef, DbDef targetDef,
        Guid? processId = null, bool dryRun = false)
    {
        _sourceDbDef = sourceDef;
        _targetDbDef = targetDef;
        _processId = processId;
        _dryRun = dryRun;
        _sourceContext = sourceDef.GetConnection();
        _targetContext = targetDef.GetConnection();
    }

    public abstract string Source { get; }
}
