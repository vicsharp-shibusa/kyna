using Kyna.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Kyna.Infrastructure.DataMigration;

internal abstract class ImportsMigratorBase
{
    protected Guid? _processId;
    protected readonly bool _dryRun;

    private protected readonly DbDef _sourceDbDef;
    private protected readonly DbDef _targetDbDef;

    public ImportsMigratorBase(DbDef sourceDef, DbDef targetDef,
        Guid? processId = null, bool dryRun = false)
    {
        _sourceDbDef = sourceDef;
        _targetDbDef = targetDef;
        _processId = processId;
        _dryRun = dryRun;
    }

    public abstract string Source { get; }
}
