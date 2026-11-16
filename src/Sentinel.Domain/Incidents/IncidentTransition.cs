namespace Sentinel.Domain.Incidents;

public static class IncidentTransitionTypes
{
    public const string Opened = "Opened";
    public const string Recovered = "Recovered";
}

public sealed class IncidentTransition
{
    private IncidentTransition()
    {
    }

    public IncidentTransition(
        Guid id,
        Guid incidentId,
        Guid monitorId,
        string transitionType,
        DateTimeOffset occurredAt,
        Guid checkResultId)
    {
        Id = id;
        IncidentId = incidentId;
        MonitorId = monitorId;
        TransitionType = transitionType;
        OccurredAt = occurredAt;
        CheckResultId = checkResultId;
    }

    public Guid Id { get; private set; }

    public Guid IncidentId { get; private set; }

    public Guid MonitorId { get; private set; }

    public string TransitionType { get; private set; } = null!;

    public DateTimeOffset OccurredAt { get; private set; }

    public Guid CheckResultId { get; private set; }
}
