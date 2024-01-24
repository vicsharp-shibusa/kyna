using System.Runtime.CompilerServices;

namespace Kyna.Infrastructure.Database;

internal sealed partial class SqlRepository(DbDef dbDef)
{
    private readonly DbDef _dbDef = dbDef;

    public SqlRepository(DatabaseEngine engine) : this(new DbDef("", engine, "")) { }

    private string ThrowSqlNotImplemented([CallerMemberName] string memberName = "")
    {
        memberName = string.IsNullOrWhiteSpace(memberName) ? "Unknown" : memberName;

        throw new NotImplementedException(
            $"Database engine {_dbDef.Engine} not implemented for {memberName}; adjust argument to {nameof(SqlRepository)} constructor.");
    }
}
