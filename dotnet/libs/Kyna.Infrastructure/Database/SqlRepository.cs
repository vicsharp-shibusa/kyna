using System.Runtime.CompilerServices;

namespace Kyna.Infrastructure.Database;

internal sealed partial class SqlRepository : SqlRepositoryBase
{
    public SqlRepository(DbDef dbDef) : base(dbDef)
    {
        ApiTransactions = new(dbDef);
        Logs = new(dbDef);
        AppEvents = new(dbDef);
    }

    public SqlRepository(DatabaseEngine engine) : this(new DbDef("", engine, "")) { }

    public ApiTransactionsInternal ApiTransactions { get; }
    public LogsInternal Logs { get; }
    public EventsInternal AppEvents { get; }
}

internal abstract class SqlRepositoryBase
{
    protected readonly DbDef _dbDef;

    public SqlRepositoryBase(DbDef dbDef)
    {
        _dbDef = dbDef;
    }

    protected string ThrowSqlNotImplemented([CallerMemberName] string memberName = "")
    {
        memberName = string.IsNullOrWhiteSpace(memberName) ? "Unknown" : memberName;

        throw new NotImplementedException(
            $"Database engine {_dbDef.Engine} not implemented for {memberName}; adjust argument to {nameof(SqlRepository)} constructor.");
    }
}