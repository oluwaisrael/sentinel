using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sentinel.Application.Monitoring;
using Sentinel.Worker;

namespace Sentinel.UnitTests.Monitoring;

public sealed class MonitoringBackgroundServiceTests
{
    [Fact]
    public async Task Processes_claimed_monitor_and_stops_gracefully()
    {
        var store = new FakeStore();
        var checker = new FakeChecker();
        await using var provider = new ServiceCollection()
            .AddScoped<IMonitorWorkStore>(_ => store)
            .AddScoped<IMonitorChecker>(_ => checker)
            .BuildServiceProvider();
        using var worker = new MonitoringBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new MonitoringOptions
            {
                PollIntervalMilliseconds = 5,
                MaxConcurrency = 1,
                LeaseDurationSeconds = 30,
                SupportedIntervalSeconds = 60
            }),
            NullLogger<MonitoringBackgroundService>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await store.Recorded.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        checker.CallCount.Should().Be(1);
        store.RecordedCount.Should().Be(1);
    }

    private sealed class FakeStore : IMonitorWorkStore
    {
        private int claimCount;
        public TaskCompletionSource<bool> Recorded { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int RecordedCount { get; private set; }

        public Task<MonitorLease?> TryClaimDueAsync(DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref claimCount) == 1)
            {
                return Task.FromResult<MonitorLease?>(new MonitorLease(
                    Guid.NewGuid(),
                    "http://203.0.113.10/health",
                    60,
                    100,
                    Guid.NewGuid(),
                    now.Add(leaseDuration)));
            }

            return Task.FromResult<MonitorLease?>(null);
        }

        public Task<bool> RecordCheckAsync(
            MonitorLease lease,
            MonitorCheckOutcome outcome,
            DateTimeOffset nextCheckAt,
            CancellationToken cancellationToken)
        {
            RecordedCount++;
            Recorded.TrySetResult(true);
            return Task.FromResult(true);
        }
    }

    private sealed class FakeChecker : IMonitorChecker
    {
        public int CallCount { get; private set; }

        public Task<MonitorCheckOutcome> CheckAsync(MonitorLease lease, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new MonitorCheckOutcome(
                DateTimeOffset.UtcNow,
                200,
                25,
                true,
                CheckErrorTypes.None));
        }
    }
}

