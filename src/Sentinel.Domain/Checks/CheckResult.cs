namespace Sentinel.Domain.Checks;

public sealed class CheckResult
{
    private CheckResult()
    {
    }

    public CheckResult(
        Guid id,
        Guid monitorId,
        DateTimeOffset checkedAt,
        int? statusCode,
        int? latencyMs,
        bool isHealthy,
        string errorType)
    {
        Id = id;
        MonitorId = monitorId;
        CheckedAt = checkedAt;
        StatusCode = statusCode;
        LatencyMs = latencyMs;
        IsHealthy = isHealthy;
        ErrorType = errorType;
    }

    public Guid Id { get; private set; }

    public Guid MonitorId { get; private set; }

    public DateTimeOffset CheckedAt { get; private set; }

    public int? StatusCode { get; private set; }

    public int? LatencyMs { get; private set; }

    public bool IsHealthy { get; private set; }

    public string ErrorType { get; private set; } = null!;
}

