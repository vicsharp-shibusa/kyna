using Kyna.Common.Events;

namespace Kyna.Infrastructure.DataMigration;

public interface IImportsMigrator
{
    string Source { get; }

    Task<TimeSpan> MigrateAsync(CancellationToken cancellationToken = default);

    event EventHandler<CommunicationEventArgs>? Communicate;

    Task<string> GetInfoAsync();
}
