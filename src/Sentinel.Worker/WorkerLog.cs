using Microsoft.Extensions.Logging;

namespace Sentinel.Worker;

internal static partial class WorkerLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Skipped check result for monitor {MonitorId}; lease {LeaseId} was lost.")]
    public static partial void LeaseLost(ILogger logger, Guid monitorId, Guid leaseId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Monitoring worker iteration failed; lease recovery will retry the monitor.")]
    public static partial void MonitoringIterationFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Notification delivery failed for event {EventId}; retry scheduled.")]
    public static partial void NotificationDeliveryFailed(ILogger logger, Exception exception, Guid eventId);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Error, Message = "Notification worker iteration failed; the outbox lease will recover.")]
    public static partial void NotificationIterationFailed(ILogger logger, Exception exception);
}
