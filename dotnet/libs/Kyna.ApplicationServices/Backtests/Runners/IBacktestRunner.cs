using Kyna.Common.Events;

namespace Kyna.ApplicationServices.Backtests.Runners;

internal interface IBacktestRunner
{
    event EventHandler<CommunicationEventArgs>? Communicate;
    Task ExecuteAsync(FileInfo configFile, CancellationToken cancellationToken);
    Task ExecuteAsync(FileInfo[] configFiles, CancellationToken cancellationToken);
}