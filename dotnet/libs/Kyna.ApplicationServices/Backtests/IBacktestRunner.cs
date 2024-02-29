using Kyna.Common.Events;

namespace Kyna.ApplicationServices.Backtests;

internal interface IBacktestRunner
{
    event EventHandler<CommunicationEventArgs>? Communicate;
    Task ExecuteAsync(CancellationToken cancellationToken);
}
