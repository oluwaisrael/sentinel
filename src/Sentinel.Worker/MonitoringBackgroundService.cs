using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentinel.Application.Monitoring;

namespace Sentinel.Worker;

public sealed class MonitoringBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<MonitoringOptions> options,
    ILogger<MonitoringBackgroundService> logger) : BackgroundService
{
    private readonly MonitoringOptions monitoringOptions = options.Value;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workers = Enumerable.Range(0, monitoringOptions.MaxConcurrency)
            .Select(_ => ProcessLoopAsync(stoppingToken));
        return Task.WhenAll(workers);
    }

    private async Task ProcessLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var store = scope.ServiceProvider.GetRequiredService<IMonitorWorkStore>();
                var checker = scope.ServiceProvider.GetRequiredService<IMonitorChecker>();
                var now = DateTimeOffset.UtcNow;
                var lease = await store.TryClaimDueAsync(
                    now,
                    TimeSpan.FromSeconds(monitoringOptions.LeaseDurationSeconds),
                    stoppingToken);

                if (lease is null)
                {
                    await Task.Delay(monitoringOptions.PollIntervalMilliseconds, stoppingToken);
                    continue;
                }

                var outcome = await checker.CheckAsync(lease, stoppingToken);
                var nextCheckAt = DateTimeOffset.UtcNow.AddSeconds(lease.IntervalSeconds);
                var recorded = await store.RecordCheckAsync(lease, outcome, nextCheckAt, stoppingToken);
                if (!recorded)
                {
                    WorkerLog.LeaseLost(logger, lease.MonitorId, lease.LeaseId);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                WorkerLog.MonitoringIterationFailed(logger, exception);
                await Task.Delay(monitoringOptions.PollIntervalMilliseconds, stoppingToken);
            }
        }
    }
}

