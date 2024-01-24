using Kyna.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace Kyna.Common.Logging;

public static class EventIdRepository
{
    private enum Events
    {
        None = 0,
        AppStarted = 100,
        AppFinished = 900
    }

    public static EventId GetAppStartedEvent(IAppConfig appConfig)
    {
        string msg = string.IsNullOrWhiteSpace(appConfig.AppName) ? "Application started"
            : $"Application {appConfig.AppName} started";

        return new EventId((int)Events.AppStarted, msg);
    }

    public static EventId GetAppFinishedEvent(IAppConfig appConfig)
    {
        string msg = string.IsNullOrWhiteSpace(appConfig.AppName) ? "Application finished"
            : $"Application {appConfig.AppName} finished";

        return new EventId((int)Events.AppFinished, msg);
    }
}
