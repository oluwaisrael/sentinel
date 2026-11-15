using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sentinel.Application.Monitoring;
using Sentinel.Domain.Checks;
using Sentinel.Infrastructure.Persistence;

namespace Sentinel.Infrastructure.Monitors;

public sealed class MonitorWorkStore(
    ApplicationDbContext dbContext,
    IOptions<MonitoringOptions> options) : IMonitorWorkStore
{
    private readonly MonitoringOptions monitoringOptions = options.Value;

    public async Task<MonitorLease?> TryClaimDueAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var leaseId = Guid.NewGuid();
        var leaseExpiresAt = now.Add(leaseDuration);

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "monitors"
            SET "LeaseId" = {leaseId}, "LeaseExpiresAt" = {leaseExpiresAt}
            WHERE "Id" = (
                SELECT "Id"
                FROM "monitors"
                WHERE "IsEnabled" = TRUE
                  AND "NextCheckAt" <= {now}
                  AND "IntervalSeconds" = {monitoringOptions.SupportedIntervalSeconds}
                  AND ("LeaseExpiresAt" IS NULL OR "LeaseExpiresAt" < {now})
                ORDER BY "NextCheckAt"
                FOR UPDATE SKIP LOCKED
                LIMIT 1
            )
            """, cancellationToken);

        var monitor = await dbContext.Monitors
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.LeaseId == leaseId, cancellationToken);

        return monitor is null
            ? null
            : new MonitorLease(
                monitor.Id,
                monitor.Url,
                monitor.IntervalSeconds,
                monitor.TimeoutMilliseconds,
                leaseId,
                leaseExpiresAt);
    }

    public async Task<bool> RecordCheckAsync(
        MonitorLease lease,
        MonitorCheckOutcome outcome,
        DateTimeOffset nextCheckAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var monitor = await dbContext.Monitors.SingleOrDefaultAsync(
            candidate => candidate.Id == lease.MonitorId,
            cancellationToken);

        if (monitor is null || !monitor.IsLeaseOwnedBy(lease.LeaseId))
        {
            return false;
        }

        dbContext.CheckResults.Add(new CheckResult(
            Guid.NewGuid(),
            lease.MonitorId,
            outcome.CheckedAt,
            outcome.StatusCode,
            outcome.LatencyMs,
            outcome.IsHealthy,
            outcome.ErrorType));
        monitor.CompleteLease(lease.LeaseId, nextCheckAt);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}

