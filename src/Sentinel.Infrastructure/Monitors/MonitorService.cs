using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Common;
using Sentinel.Application.Monitors;
using Sentinel.Domain.Monitors;
using Sentinel.Infrastructure.Persistence;
using DomainMonitor = Sentinel.Domain.Monitors.Monitor;

namespace Sentinel.Infrastructure.Monitors;

public sealed class MonitorService(ApplicationDbContext dbContext) : IMonitorService
{
    public async Task<MonitorResponse> CreateAsync(
        string ownerId,
        CreateMonitorRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAllowedUrl(request.Url);

        var monitor = DomainMonitor.Create(
            ownerId,
            request.Name,
            request.Url,
            request.IntervalSeconds,
            request.TimeoutMilliseconds);

        dbContext.Monitors.Add(monitor);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(monitor);
    }

    public async Task<IReadOnlyList<MonitorResponse>> ListAsync(string ownerId, CancellationToken cancellationToken)
    {
        return await dbContext.Monitors
            .AsNoTracking()
            .Where(monitor => monitor.OwnerId == ownerId)
            .OrderBy(monitor => monitor.Name)
            .Select(monitor => new MonitorResponse(
                monitor.Id,
                monitor.Name,
                monitor.Url,
                monitor.IntervalSeconds,
                monitor.TimeoutMilliseconds,
                monitor.IsEnabled,
                monitor.CreatedAt,
                monitor.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<MonitorResponse> GetAsync(string ownerId, Guid monitorId, CancellationToken cancellationToken)
    {
        var monitor = await GetOwnedAsync(ownerId, monitorId, cancellationToken);
        return Map(monitor);
    }

    public async Task<MonitorResponse> UpdateAsync(
        string ownerId,
        Guid monitorId,
        UpdateMonitorRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAllowedUrl(request.Url);

        var monitor = await GetOwnedAsync(ownerId, monitorId, cancellationToken);
        monitor.Update(
            request.Name,
            request.Url,
            request.IntervalSeconds,
            request.TimeoutMilliseconds,
            request.IsEnabled);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(monitor);
    }

    public async Task DeleteAsync(string ownerId, Guid monitorId, CancellationToken cancellationToken)
    {
        var monitor = await GetOwnedAsync(ownerId, monitorId, cancellationToken);
        dbContext.Monitors.Remove(monitor);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<DomainMonitor> GetOwnedAsync(string ownerId, Guid monitorId, CancellationToken cancellationToken)
    {
        var monitor = await dbContext.Monitors
            .SingleOrDefaultAsync(
                candidate => candidate.Id == monitorId && candidate.OwnerId == ownerId,
                cancellationToken);

        return monitor ?? throw new NotFoundException("Monitor was not found.");
    }

    private static void EnsureAllowedUrl(string rawUrl)
    {
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) || !MonitorUrlPolicy.IsAllowed(uri))
        {
            throw new ArgumentException("The monitor URL is not allowed.", nameof(rawUrl));
        }
    }

    private static MonitorResponse Map(DomainMonitor monitor) => new(
        monitor.Id,
        monitor.Name,
        monitor.Url,
        monitor.IntervalSeconds,
        monitor.TimeoutMilliseconds,
        monitor.IsEnabled,
        monitor.CreatedAt,
        monitor.UpdatedAt);
}

