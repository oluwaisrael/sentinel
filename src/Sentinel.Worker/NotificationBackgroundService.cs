using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentinel.Application.Notifications;

namespace Sentinel.Worker;

public sealed class NotificationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationOptions> options,
    ILogger<NotificationBackgroundService> logger) : BackgroundService
{
    private readonly NotificationOptions notificationOptions = options.Value;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workers = Enumerable.Range(0, notificationOptions.MaxConcurrency)
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
                var store = scope.ServiceProvider.GetRequiredService<INotificationWorkStore>();
                var provider = scope.ServiceProvider.GetRequiredService<IEmailProvider>();
                var lease = await store.TryClaimDueAsync(
                    DateTimeOffset.UtcNow,
                    TimeSpan.FromSeconds(notificationOptions.LeaseDurationSeconds),
                    notificationOptions.MaxAttempts,
                    stoppingToken);

                if (lease is null)
                {
                    await Task.Delay(notificationOptions.PollIntervalMilliseconds, stoppingToken);
                    continue;
                }

                try
                {
                    await provider.SendAsync(
                        new IncidentNotification(
                            lease.Id,
                            lease.IncidentId,
                            lease.MonitorId,
                            lease.EventType,
                            lease.Recipient,
                            lease.Subject,
                            lease.Body),
                        stoppingToken);
                    await store.MarkSentAsync(lease, DateTimeOffset.UtcNow, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    var delaySeconds = Math.Min(
                        3_600,
                        notificationOptions.RetryBaseDelaySeconds * Math.Pow(2, Math.Max(0, lease.AttemptCount - 1)));
                    await store.MarkFailedAsync(
                        lease,
                        DateTimeOffset.UtcNow.AddSeconds(delaySeconds),
                        exception.Message,
                        stoppingToken);
                    WorkerLog.NotificationDeliveryFailed(logger, exception, lease.Id);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                WorkerLog.NotificationIterationFailed(logger, exception);
                await Task.Delay(notificationOptions.PollIntervalMilliseconds, stoppingToken);
            }
        }
    }
}
