namespace Sentinel.Application.Monitors;

public interface IMonitorService
{
    Task<MonitorResponse> CreateAsync(string ownerId, CreateMonitorRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<MonitorResponse>> ListAsync(string ownerId, CancellationToken cancellationToken);

    Task<MonitorResponse> GetAsync(string ownerId, Guid monitorId, CancellationToken cancellationToken);

    Task<MonitorResponse> UpdateAsync(string ownerId, Guid monitorId, UpdateMonitorRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(string ownerId, Guid monitorId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MonitorCheckResponse>> GetChecksAsync(
        string ownerId,
        Guid monitorId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        CancellationToken cancellationToken);

    Task<MonitorStatsResponse> GetStatsAsync(
        string ownerId,
        Guid monitorId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);
}

