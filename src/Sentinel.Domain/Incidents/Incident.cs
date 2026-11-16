using Sentinel.Domain.Common;

namespace Sentinel.Domain.Incidents;

public static class IncidentStatuses
{
    public const string Open = "Open";
    public const string Recovered = "Recovered";
}

public sealed class Incident
{
    private Incident()
    {
    }

    private Incident(Guid id, Guid monitorId, DateTimeOffset openedAt)
    {
        Id = id;
        MonitorId = monitorId;
        Status = IncidentStatuses.Open;
        OpenedAt = openedAt;
        LastTransitionAt = openedAt;
    }

    public Guid Id { get; private set; }

    public Guid MonitorId { get; private set; }

    public string Status { get; private set; } = null!;

    public DateTimeOffset OpenedAt { get; private set; }

    public DateTimeOffset? RecoveredAt { get; private set; }

    public DateTimeOffset LastTransitionAt { get; private set; }

    public static Incident Open(Guid monitorId, DateTimeOffset openedAt)
    {
        if (monitorId == Guid.Empty)
        {
            throw new DomainException("Monitor ID is required.");
        }

        return new Incident(Guid.NewGuid(), monitorId, openedAt);
    }

    public void Recover(DateTimeOffset recoveredAt)
    {
        if (Status != IncidentStatuses.Open)
        {
            throw new DomainException("Only open incidents can be recovered.");
        }

        Status = IncidentStatuses.Recovered;
        RecoveredAt = recoveredAt;
        LastTransitionAt = recoveredAt;
    }
}
