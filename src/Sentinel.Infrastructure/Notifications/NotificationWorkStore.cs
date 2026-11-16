using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sentinel.Application.Notifications;
using Sentinel.Domain.Notifications;
using Sentinel.Infrastructure.Persistence;

namespace Sentinel.Infrastructure.Notifications;

public sealed class NotificationWorkStore(
    ApplicationDbContext dbContext) : INotificationWorkStore
{
    public async Task<NotificationLease?> TryClaimDueAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        var leaseId = Guid.NewGuid();
        var leaseExpiresAt = now.Add(leaseDuration);

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "notification_outbox"
            SET "LeaseId" = {leaseId},
                "LeaseExpiresAt" = {leaseExpiresAt},
                "Status" = {NotificationStatuses.Processing},
                "AttemptCount" = "AttemptCount" + 1,
                "LastError" = NULL
            WHERE "Id" = (
                SELECT "Id"
                FROM "notification_outbox"
                WHERE "AttemptCount" < {maxAttempts}
                  AND (
                      ("Status" IN ({NotificationStatuses.Pending}, {NotificationStatuses.Failed}) AND "NextAttemptAt" <= {now})
                      OR ("Status" = {NotificationStatuses.Processing} AND "LeaseExpiresAt" < {now})
                  )
                ORDER BY "NextAttemptAt", "CreatedAt"
                FOR UPDATE SKIP LOCKED
                LIMIT 1
            )
            """, cancellationToken);

        var notification = await (
            from item in dbContext.NotificationOutbox.AsNoTracking()
            join monitor in dbContext.Monitors.AsNoTracking() on item.MonitorId equals monitor.Id
            join user in dbContext.Users.AsNoTracking() on monitor.OwnerId equals user.Id
            where item.LeaseId == leaseId
            select new
            {
                item.Id,
                item.IncidentId,
                item.MonitorId,
                item.EventType,
                item.AttemptCount,
                MonitorName = monitor.Name,
                MonitorUrl = monitor.Url,
                Recipient = user.Email
            }).SingleOrDefaultAsync(cancellationToken);

        if (notification is null)
        {
            return null;
        }

        var isRecovery = notification.EventType == NotificationEventTypes.IncidentRecovered;
        var subject = isRecovery
            ? $"Sentinel recovered: {notification.MonitorName}"
            : $"Sentinel outage: {notification.MonitorName}";
        var body = isRecovery
            ? $"The monitored endpoint {notification.MonitorUrl} has recovered."
            : $"The monitored endpoint {notification.MonitorUrl} is experiencing an outage.";

        return new NotificationLease(
            notification.Id,
            notification.IncidentId,
            notification.MonitorId,
            notification.EventType,
            leaseId,
            leaseExpiresAt,
            notification.AttemptCount,
            notification.Recipient ?? string.Empty,
            subject,
            body);
    }

    public async Task<bool> MarkSentAsync(
        NotificationLease lease,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken)
    {
        var notification = await dbContext.NotificationOutbox
            .SingleOrDefaultAsync(item => item.Id == lease.Id, cancellationToken);
        if (notification is null || !notification.IsLeaseOwnedBy(lease.LeaseId))
        {
            return false;
        }

        notification.MarkSent(lease.LeaseId, sentAt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarkFailedAsync(
        NotificationLease lease,
        DateTimeOffset nextAttemptAt,
        string failureReason,
        CancellationToken cancellationToken)
    {
        var notification = await dbContext.NotificationOutbox
            .SingleOrDefaultAsync(item => item.Id == lease.Id, cancellationToken);
        if (notification is null || !notification.IsLeaseOwnedBy(lease.LeaseId))
        {
            return false;
        }

        notification.MarkFailed(lease.LeaseId, nextAttemptAt, failureReason);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
