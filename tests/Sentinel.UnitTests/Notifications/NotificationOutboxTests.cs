using FluentAssertions;
using Sentinel.Domain.Notifications;

namespace Sentinel.UnitTests.Notifications;

public sealed class NotificationOutboxTests
{
    [Fact]
    public void Claim_increments_attempt_and_mark_sent_clears_lease()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var outbox = new NotificationOutbox(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), NotificationEventTypes.IncidentOpened, createdAt);
        var leaseId = Guid.NewGuid();
        outbox.Claim(leaseId, createdAt.AddMinutes(1));

        outbox.AttemptCount.Should().Be(1);
        outbox.Status.Should().Be(NotificationStatuses.Processing);

        outbox.MarkSent(leaseId, createdAt.AddSeconds(1));

        outbox.Status.Should().Be(NotificationStatuses.Sent);
        outbox.LeaseId.Should().BeNull();
        outbox.SentAt.Should().NotBeNull();
    }

    [Fact]
    public void Mark_failed_preserves_bounded_error_and_schedules_retry()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var outbox = new NotificationOutbox(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), NotificationEventTypes.IncidentRecovered, createdAt);
        var leaseId = Guid.NewGuid();
        outbox.Claim(leaseId, createdAt.AddMinutes(1));

        outbox.MarkFailed(leaseId, createdAt.AddMinutes(2), new string('x', 3_000));

        outbox.Status.Should().Be(NotificationStatuses.Failed);
        outbox.LastError.Should().HaveLength(2_000);
        outbox.NextAttemptAt.Should().Be(createdAt.AddMinutes(2));
    }
}
