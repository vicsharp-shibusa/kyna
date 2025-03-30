using Kyna.Infrastructure.Events;

namespace Kyna.Infrastructure.DataImport;

public interface IExternalDataImporter
{
    string Source { get; }

    Task<TimeSpan> ImportAsync(CancellationToken cancellationToken = default);

    event EventHandler<CommunicationEventArgs>? Communicate;

    Task<string> GetInfoAsync();

    (bool IsDangerous, string[] DangerMessages) ContainsDanger();

    void Dispose();
}
