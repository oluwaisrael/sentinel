using FluentAssertions;
using Sentinel.Domain.Common;
using Sentinel.Domain.Incidents;

namespace Sentinel.UnitTests.Incidents;

public sealed class IncidentTests
{
    [Fact]
    public void Open_creates_open_incident()
    {
        var openedAt = DateTimeOffset.UtcNow;
        var incident = Incident.Open(Guid.NewGuid(), openedAt);

        incident.Status.Should().Be(IncidentStatuses.Open);
        incident.OpenedAt.Should().Be(openedAt);
        incident.RecoveredAt.Should().BeNull();
    }

    [Fact]
    public void Recover_closes_open_incident()
    {
        var incident = Incident.Open(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-1));
        var recoveredAt = DateTimeOffset.UtcNow;

        incident.Recover(recoveredAt);

        incident.Status.Should().Be(IncidentStatuses.Recovered);
        incident.RecoveredAt.Should().Be(recoveredAt);
    }

    [Fact]
    public void Recover_rejects_already_recovered_incident()
    {
        var incident = Incident.Open(Guid.NewGuid(), DateTimeOffset.UtcNow);
        incident.Recover(DateTimeOffset.UtcNow);

        var action = () => incident.Recover(DateTimeOffset.UtcNow);

        action.Should().Throw<DomainException>();
    }
}
