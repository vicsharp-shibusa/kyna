namespace Kyna.Infrastructure.Database;

internal partial class SqlRepository
{
    internal class RemoteFilesInternal(DbDef dbDef) : SqlRepositoryBase(dbDef)
    {
        public string Upsert => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
INSERT INTO public.remote_files(
source, provider, location, name, update_date, size, hash_code, process_id, 
created_ticks_utc, updated_ticks_utc)
VALUES (@Source, @Provider, @Location, @Name, @UpdateDate, @Size, @HashCode, @ProcessId,
@CreatedTicksUtc, @UpdatedTicksUtc)
ON CONFLICT (source, provider, location, name) DO UPDATE SET
update_date = EXCLUDED.update_date,
size = EXCLUDED.size,
hash_code = EXCLUDED.hash_code,
process_id = EXCLUDED.process_id,
updated_ticks_utc = EXCLUDED.updated_ticks_utc
",
            _ => ThrowSqlNotImplemented()
        };

        public string Fetch => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"
SELECT source, provider, location, name, update_date AS UpdateDate, size, hash_code AS HashCode,
process_id AS ProcessId, 
created_ticks_utc AS CreatedTicksUtc, updated_ticks_utc AS UpdatedTicksUtc
FROM public.remote_files
",
            _ => ThrowSqlNotImplemented()
        };

        public string DeleteForSource => _dbDef.Engine switch
        {
            DatabaseEngine.PostgreSql => @"DELETE FROM public.remote_files WHERE source = @Source",
            _ => ThrowSqlNotImplemented()
        };
    }
}