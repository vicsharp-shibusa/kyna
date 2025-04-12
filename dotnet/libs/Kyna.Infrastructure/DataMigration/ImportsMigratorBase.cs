using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.DataImport;
using Kyna.Infrastructure.Events;
using System.Text;

namespace Kyna.Infrastructure.DataMigration;

internal abstract class ImportsMigratorBase
{
    protected Guid? _processId;
    protected readonly bool _dryRun;

    private protected readonly DbDef _sourceDbDef;
    private protected readonly DbDef _targetDbDef;

    public event EventHandler<CommunicationEventArgs>? Communicate;

    public ImportsMigratorBase(DbDef sourceDef, DbDef targetDef,
        Guid? processId = null, bool dryRun = false)
    {
        _sourceDbDef = sourceDef;
        _targetDbDef = targetDef;
        _processId = processId;
        _dryRun = dryRun;
    }

    public abstract string Source { get; }

    protected virtual void Printf(string message)
    {
        if (!string.IsNullOrWhiteSpace(message) && Communicate != null)
        {
            StringBuilder result = new();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            result.Append($"[{DateTimeOffset.Now}]\t");
            result.Append(message.Trim());
            Communicate?.Invoke(this, new CommunicationEventArgs(result.ToString(), nameof(PolygonImporter)));
        }
    }
}