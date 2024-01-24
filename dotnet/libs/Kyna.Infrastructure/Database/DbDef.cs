namespace Kyna.Infrastructure.Database;

public struct DbDef(string name, DatabaseEngine engine, string connectionString)
{
    public string Name = name;
    public DatabaseEngine Engine = engine;
    public string ConnectionString = connectionString;
}
