using Kyna.Common;
using System.Runtime.CompilerServices;

namespace Kyna.Infrastructure.Database;

internal sealed partial class SqlRepository(DbDef dbDef) : SqlRepositoryBase(dbDef)
{
    public SqlRepository(DatabaseEngine engine) : this(new DbDef("", engine, "")) { }
    public ApiTransactionsInternal ApiTransactions { get; } = new(dbDef);
    public LogsInternal Logs { get; } = new(dbDef);
    public EventsInternal AppEvents { get; } = new(dbDef);
    public EodPricesInternal EodPrices { get; } = new(dbDef);
    public AdjustedEodPricesInternal AdjustedEodPrices { get; } = new(dbDef);
    public SplitsInternal Splits { get; } = new(dbDef);
    public BacktestsInternal Backtests { get; } = new(dbDef);
    public FundamentalsInternal Fundamentals { get; } = new(dbDef);

    public string GetInCollectionSql(string parameterName)
    {
        parameterName = parameterName.Trim();
        if (!parameterName.StartsWith('@'))
        {
            parameterName = $"@{parameterName}";
        }

        return _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => $" = Any({parameterName})",
            DatabaseEngine.MsSqlServer => $" IN {parameterName}",
            _ => throw new Exception($"Unknown db engine, {_dbDef.Engine.GetEnumDescription()}, in {nameof(SqlRepository)}.{nameof(GetInCollectionSql)}")
        };
    }
}

internal abstract class SqlRepositoryBase(DbDef dbDef)
{
    protected readonly DbDef _dbDef = dbDef;

    protected string ThrowSqlNotImplemented([CallerMemberName] string memberName = "")
    {
        memberName = string.IsNullOrWhiteSpace(memberName) ? "Unknown" : memberName;

        throw new NotImplementedException(
            $"Database engine {_dbDef.Engine} not implemented for {memberName}; adjust argument to {nameof(SqlRepository)} constructor.");
    }
}