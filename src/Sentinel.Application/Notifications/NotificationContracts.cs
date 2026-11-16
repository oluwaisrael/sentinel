namespace Sentinel.Application.Notifications;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public int PollIntervalMilliseconds { get; init; } = 1_000;

    public int MaxConcurrency { get; init; } = 2;

    public int LeaseDurationSeconds { get; init; } = 60;

    public int MaxAttempts { get; init; } = 5;

    public int RetryBaseDelaySeconds { get; init; } = 30;
}

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string From { get; init; } = string.Empty;

    public bool UseSsl { get; init; } = true;
}

public sealed record NotificationLease(
    Guid Id,
    Guid IncidentId,
    Guid MonitorId,
    string EventType,
    Guid LeaseId,
    DateTimeOffset LeaseExpiresAt,
    int AttemptCount,
    string Recipient,
    string Subject,
    string Body);

public sealed record IncidentNotification(
    Guid EventId,
    Guid IncidentId,
    Guid MonitorId,
    string EventType,
    string Recipient,
    string Subject,
    string Body);

public interface INotificationWorkStore
{
    Task<NotificationLease?> TryClaimDueAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        int maxAttempts,
        CancellationToken cancellationToken);

    Task<bool> MarkSentAsync(NotificationLease lease, DateTimeOffset sentAt, CancellationToken cancellationToken);

    Task<bool> MarkFailedAsync(
        NotificationLease lease,
        DateTimeOffset nextAttemptAt,
        string failureReason,
        CancellationToken cancellationToken);
}

public interface IEmailProvider
{
    Task SendAsync(IncidentNotification notification, CancellationToken cancellationToken);
}
