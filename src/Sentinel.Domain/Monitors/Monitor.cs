using Sentinel.Domain.Common;

namespace Sentinel.Domain.Monitors;

public sealed class Monitor
{
    private Monitor()
    {
    }

    private Monitor(Guid id, string ownerId, string name, string url, int intervalSeconds, int timeoutMilliseconds)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        Url = url;
        IntervalSeconds = intervalSeconds;
        TimeoutMilliseconds = timeoutMilliseconds;
        IsEnabled = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public string OwnerId { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string Url { get; private set; } = null!;

    public int IntervalSeconds { get; private set; }

    public int TimeoutMilliseconds { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Monitor Create(string ownerId, string name, string url, int intervalSeconds, int timeoutMilliseconds)
    {
        ValidateOwner(ownerId);
        ValidateText(name, nameof(name), 1, 100);
        ValidateText(url, nameof(url), 1, 2048);
        ValidateRange(intervalSeconds, 30, 86_400, nameof(intervalSeconds));
        ValidateRange(timeoutMilliseconds, 100, 120_000, nameof(timeoutMilliseconds));

        return new Monitor(Guid.NewGuid(), ownerId, name.Trim(), url.Trim(), intervalSeconds, timeoutMilliseconds);
    }

    public void Update(string name, string url, int intervalSeconds, int timeoutMilliseconds, bool isEnabled)
    {
        ValidateText(name, nameof(name), 1, 100);
        ValidateText(url, nameof(url), 1, 2048);
        ValidateRange(intervalSeconds, 30, 86_400, nameof(intervalSeconds));
        ValidateRange(timeoutMilliseconds, 100, 120_000, nameof(timeoutMilliseconds));

        Name = name.Trim();
        Url = url.Trim();
        IntervalSeconds = intervalSeconds;
        TimeoutMilliseconds = timeoutMilliseconds;
        IsEnabled = isEnabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateOwner(string ownerId) => ValidateText(ownerId, nameof(ownerId), 1, 450);

    private static void ValidateText(string value, string parameterName, int minimumLength, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < minimumLength || value.Trim().Length > maximumLength)
        {
            throw new DomainException($"{parameterName} must contain between {minimumLength} and {maximumLength} characters.");
        }
    }

    private static void ValidateRange(int value, int minimum, int maximum, string parameterName)
    {
        if (value is < minimum or > maximum)
        {
            throw new DomainException($"{parameterName} must be between {minimum} and {maximum}.");
        }
    }
}

