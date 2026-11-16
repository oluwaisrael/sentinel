namespace Sentinel.Domain.Notifications;

public static class NotificationEventTypes
{
    public const string IncidentOpened = "IncidentOpened";
    public const string IncidentRecovered = "IncidentRecovered";
}

public static class NotificationStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
}

public sealed class NotificationOutbox
{
    private NotificationOutbox()
    {
    }

    public NotificationOutbox(
        Guid id,
        Guid incidentId,
        Guid monitorId,
        string eventType,
        DateTimeOffset occurredAt)
    {
        Id = id;
        IncidentId = incidentId;
        MonitorId = monitorId;
        EventType = eventType;
        OccurredAt = occurredAt;
        CreatedAt = occurredAt;
        NextAttemptAt = occurredAt;
        Status = NotificationStatuses.Pending;
    }

    public Guid Id { get; private set; }

    public Guid IncidentId { get; private set; }

    public Guid MonitorId { get; private set; }

    public string EventType { get; private set; } = null!;

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string Status { get; private set; } = null!;

    public int AttemptCount { get; private set; }

    public DateTimeOffset NextAttemptAt { get; private set; }

    public Guid? LeaseId { get; private set; }

    public DateTimeOffset? LeaseExpiresAt { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    public string? LastError { get; private set; }

    public void Claim(Guid leaseId, DateTimeOffset leaseExpiresAt)
    {
        LeaseId = leaseId;
        LeaseExpiresAt = leaseExpiresAt;
        Status = NotificationStatuses.Processing;
        AttemptCount++;
        LastError = null;
    }

    public bool IsLeaseOwnedBy(Guid leaseId) => LeaseId == leaseId;

    public void MarkSent(Guid leaseId, DateTimeOffset sentAt)
    {
        EnsureLease(leaseId);
        Status = NotificationStatuses.Sent;
        SentAt = sentAt;
        LeaseId = null;
        LeaseExpiresAt = null;
        LastError = null;
    }

    public void MarkFailed(Guid leaseId, DateTimeOffset nextAttemptAt, string error)
    {
        EnsureLease(leaseId);
        Status = NotificationStatuses.Failed;
        NextAttemptAt = nextAttemptAt;
        LastError = error[..Math.Min(error.Length, 2_000)];
        LeaseId = null;
        LeaseExpiresAt = null;
    }

    private void EnsureLease(Guid leaseId)
    {
        if (!IsLeaseOwnedBy(leaseId))
        {
            throw new InvalidOperationException("The notification lease is no longer owned by this worker.");
        }
    }
}
