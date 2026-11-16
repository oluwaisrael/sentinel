using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Incidents;
using Sentinel.Infrastructure.Persistence;

namespace Sentinel.Infrastructure.Incidents;

public sealed class IncidentService(ApplicationDbContext dbContext) : IIncidentService
{
    public async Task<IReadOnlyList<IncidentResponse>> ListAsync(
        string ownerId,
        Guid monitorId,
        DateTimeOffset from,
        DateTimeOffset windowEnd,
        int limit,
        CancellationToken cancellationToken)
    {
        var monitorExists = await dbContext.Monitors
            .AnyAsync(monitor => monitor.Id == monitorId && monitor.OwnerId == ownerId, cancellationToken);
        if (!monitorExists)
        {
            throw new KeyNotFoundException("Monitor was not found.");
        }

        return await dbContext.Incidents
            .AsNoTracking()
            .Where(incident => incident.MonitorId == monitorId &&
                               incident.OpenedAt >= from &&
                               incident.OpenedAt <= windowEnd)
            .OrderByDescending(incident => incident.OpenedAt)
            .Take(limit)
            .Select(incident => new IncidentResponse(
                incident.Id,
                incident.MonitorId,
                incident.Status,
                incident.OpenedAt,
                incident.RecoveredAt,
                incident.LastTransitionAt))
            .ToListAsync(cancellationToken);
    }
}
