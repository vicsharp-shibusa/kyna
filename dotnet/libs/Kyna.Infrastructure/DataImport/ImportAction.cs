namespace Kyna.Infrastructure.DataImport;

internal struct ImportAction(string? name, string[]? details)
{
    public string? Name = name;
    public string[]? Details = details;

    public static ImportAction Default => new(null, null);
}
