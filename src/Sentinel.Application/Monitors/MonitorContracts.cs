using System.ComponentModel.DataAnnotations;

namespace Sentinel.Application.Monitors;

public sealed record CreateMonitorRequest(
    [property: Required, StringLength(100, MinimumLength = 1)] string Name,
    [property: Required, StringLength(2048)] string Url,
    [property: Range(30, 86_400)] int IntervalSeconds = 300,
    [property: Range(100, 120_000)] int TimeoutMilliseconds = 10_000);

public sealed record UpdateMonitorRequest(
    [property: Required, StringLength(100, MinimumLength = 1)] string Name,
    [property: Required, StringLength(2048)] string Url,
    [property: Range(30, 86_400)] int IntervalSeconds = 300,
    [property: Range(100, 120_000)] int TimeoutMilliseconds = 10_000,
    bool IsEnabled = true);

public sealed record MonitorResponse(
    Guid Id,
    string Name,
    string Url,
    int IntervalSeconds,
    int TimeoutMilliseconds,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

