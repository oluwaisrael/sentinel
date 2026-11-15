using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Common;
using Sentinel.Application.Monitoring;
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

        var duplicateName = await dbContext.Monitors.AnyAsync(
            monitor => monitor.OwnerId == ownerId && monitor.Name == request.Name.Trim(),
            cancellationToken);
        if (duplicateName)
        {
            throw new ConflictException("A monitor with this name already exists.");
        }

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
        var duplicateName = await dbContext.Monitors.AnyAsync(
            candidate => candidate.OwnerId == ownerId && candidate.Id != monitorId && candidate.Name == request.Name.Trim(),
            cancellationToken);
        if (duplicateName)
        {
            throw new ConflictException("A monitor with this name already exists.");
        }

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

    public async Task<IReadOnlyList<MonitorCheckResponse>> GetChecksAsync(
        string ownerId,
        Guid monitorId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        CancellationToken cancellationToken)
    {
        await GetOwnedAsync(ownerId, monitorId, cancellationToken);

        var query = dbContext.CheckResults
            .AsNoTracking()
            .Where(result => result.MonitorId == monitorId);

        if (from.HasValue)
        {
            query = query.Where(result => result.CheckedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(result => result.CheckedAt <= to.Value);
        }

        return await query
            .OrderByDescending(result => result.CheckedAt)
            .Take(limit)
            .Select(result => new MonitorCheckResponse(
                result.CheckedAt,
                result.StatusCode,
                result.LatencyMs,
                result.IsHealthy,
                result.ErrorType))
            .ToListAsync(cancellationToken);
    }

    public async Task<MonitorStatsResponse> GetStatsAsync(
        string ownerId,
        Guid monitorId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        await GetOwnedAsync(ownerId, monitorId, cancellationToken);

        var query = dbContext.CheckResults
            .AsNoTracking()
            .Where(result => result.MonitorId == monitorId && result.CheckedAt >= from && result.CheckedAt <= to);
        var totalChecks = await query.CountAsync(cancellationToken);
        var healthyChecks = await query.CountAsync(result => result.IsHealthy, cancellationToken);
        var averageLatency = await query
            .Where(result => result.LatencyMs.HasValue)
            .AverageAsync(result => (double?)result.LatencyMs, cancellationToken);

        return new MonitorStatsResponse(
            from,
            to,
            totalChecks,
            healthyChecks,
            totalChecks == 0 ? 0 : healthyChecks * 100d / totalChecks,
            averageLatency);
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

