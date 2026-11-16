namespace Sentinel.Application.Incidents;

public sealed record IncidentResponse(
    Guid Id,
    Guid MonitorId,
    string Status,
    DateTimeOffset OpenedAt,
    DateTimeOffset? RecoveredAt,
    DateTimeOffset LastTransitionAt);

public interface IIncidentService
{
    Task<IReadOnlyList<IncidentResponse>> ListAsync(
        string ownerId,
        Guid monitorId,
        DateTimeOffset from,
        DateTimeOffset windowEnd,
        int limit,
        CancellationToken cancellationToken);
}
