namespace Sentinel.Application.Monitoring;

public static class CheckErrorTypes
{
    public const string None = "None";
    public const string Timeout = "Timeout";
    public const string DnsFailure = "DnsFailure";
    public const string ConnectionFailure = "ConnectionFailure";
    public const string Cancelled = "Cancelled";
    public const string BlockedDestination = "BlockedDestination";
    public const string Unexpected = "Unexpected";
}

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public int PollIntervalMilliseconds { get; init; } = 1_000;

    public int MaxConcurrency { get; init; } = 4;

    public int LeaseDurationSeconds { get; init; } = 30;

    public int SupportedIntervalSeconds { get; init; } = 60;

    public int IncidentFailureThreshold { get; init; } = 3;

    public int IncidentRecoveryThreshold { get; init; } = 2;
}

public sealed record MonitorLease(
    Guid MonitorId,
    string Url,
    int IntervalSeconds,
    int TimeoutMilliseconds,
    Guid LeaseId,
    DateTimeOffset LeaseExpiresAt);

public sealed record MonitorCheckOutcome(
    DateTimeOffset CheckedAt,
    int? StatusCode,
    int? LatencyMs,
    bool IsHealthy,
    string ErrorType);

public interface IMonitorWorkStore
{
    Task<MonitorLease?> TryClaimDueAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task<bool> RecordCheckAsync(
        MonitorLease lease,
        MonitorCheckOutcome outcome,
        DateTimeOffset nextCheckAt,
        CancellationToken cancellationToken);
}

public interface IMonitorChecker
{
    Task<MonitorCheckOutcome> CheckAsync(MonitorLease lease, CancellationToken cancellationToken);
}

