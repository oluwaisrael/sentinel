using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sentinel.Application.Notifications;
using Sentinel.Worker;

namespace Sentinel.UnitTests.Notifications;

public sealed class NotificationBackgroundServiceTests
{
    [Fact]
    public async Task Provider_failure_is_recorded_without_crashing_worker()
    {
        var store = new FakeStore();
        await using var provider = new ServiceCollection()
            .AddScoped<INotificationWorkStore>(_ => store)
            .AddScoped<IEmailProvider, FailingProvider>()
            .BuildServiceProvider();
        using var worker = new NotificationBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new NotificationOptions
            {
                PollIntervalMilliseconds = 5,
                MaxConcurrency = 1,
                LeaseDurationSeconds = 30,
                MaxAttempts = 5,
                RetryBaseDelaySeconds = 1
            }),
            NullLogger<NotificationBackgroundService>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await store.Failed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        store.FailedCount.Should().Be(1);
        store.LastFailure.Should().Be("provider unavailable");
    }

    private sealed class FakeStore : INotificationWorkStore
    {
        private int claimCount;
        public TaskCompletionSource<bool> Failed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int FailedCount { get; private set; }
        public string? LastFailure { get; private set; }

        public Task<NotificationLease?> TryClaimDueAsync(DateTimeOffset now, TimeSpan leaseDuration, int maxAttempts, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref claimCount) == 1)
            {
                return Task.FromResult<NotificationLease?>(new NotificationLease(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "IncidentOpened", Guid.NewGuid(), now.Add(leaseDuration), 1,
                    "owner@example.com", "Outage", "Outage"));
            }

            return Task.FromResult<NotificationLease?>(null);
        }

        public Task<bool> MarkSentAsync(NotificationLease lease, DateTimeOffset sentAt, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<bool> MarkFailedAsync(NotificationLease lease, DateTimeOffset nextAttemptAt, string failureReason, CancellationToken cancellationToken)
        {
            FailedCount++;
            LastFailure = failureReason;
            Failed.TrySetResult(true);
            return Task.FromResult(true);
        }
    }

    private sealed class FailingProvider : IEmailProvider
    {
        public Task SendAsync(IncidentNotification notification, CancellationToken cancellationToken)
            => Task.FromException(new InvalidOperationException("provider unavailable"));
    }
}
