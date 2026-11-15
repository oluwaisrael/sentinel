using System.ComponentModel.DataAnnotations;

namespace Sentinel.Application.Monitors;

public sealed class CreateMonitorRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(2048)]
    public string Url { get; init; } = string.Empty;

    [Range(30, 86_400)]
    public int IntervalSeconds { get; init; } = 300;

    [Range(100, 120_000)]
    public int TimeoutMilliseconds { get; init; } = 10_000;
}

public sealed class UpdateMonitorRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(2048)]
    public string Url { get; init; } = string.Empty;

    [Range(30, 86_400)]
    public int IntervalSeconds { get; init; } = 300;

    [Range(100, 120_000)]
    public int TimeoutMilliseconds { get; init; } = 10_000;

    public bool IsEnabled { get; init; } = true;
}

public sealed record MonitorResponse(
    Guid Id,
    string Name,
    string Url,
    int IntervalSeconds,
    int TimeoutMilliseconds,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

