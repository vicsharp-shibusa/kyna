using Kyna.Infrastructure.Events;

namespace Kyna.Infrastructure.DataMigration;

public interface IImportsMigrator
{
    event EventHandler<CommunicationEventArgs>? Communicate;
    string Source { get; }
    Task<TimeSpan> MigrateAsync(CancellationToken cancellationToken = default);
    string GetInfo();
}
