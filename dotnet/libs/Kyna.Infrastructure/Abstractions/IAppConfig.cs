namespace Kyna.Infrastructure.Abstractions;

public interface IAppConfig
{
    string AppName { get; }
    string AppVersion { get; }
    string? Description { get; }
    bool Verbose { get; set; }
    bool ShowHelp { get; set; }
}
