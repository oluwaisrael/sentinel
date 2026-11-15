using FluentAssertions;
using Sentinel.Domain.Common;
using Sentinel.Domain.Monitors;

namespace Sentinel.UnitTests.Monitors;

public sealed class MonitorTests
{
    [Fact]
    public void Create_with_valid_values_creates_enabled_monitor()
    {
        var monitor = Monitor.Create("user-1", "Public API", "https://api.example.com/health", 60, 5_000);

        monitor.OwnerId.Should().Be("user-1");
        monitor.Name.Should().Be("Public API");
        monitor.IsEnabled.Should().BeTrue();
        monitor.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(29)]
    [InlineData(86_401)]
    public void Create_rejects_interval_outside_supported_range(int intervalSeconds)
    {
        var action = () => Monitor.Create("user-1", "API", "https://api.example.com", intervalSeconds, 5_000);

        action.Should().Throw<DomainException>().WithMessage("*intervalSeconds*");
    }

    [Fact]
    public void Update_changes_configuration_and_state()
    {
        var monitor = Monitor.Create("user-1", "API", "https://api.example.com", 60, 5_000);

        monitor.Update("Updated API", "https://status.example.com", 300, 10_000, false);

        monitor.Name.Should().Be("Updated API");
        monitor.Url.Should().Be("https://status.example.com");
        monitor.IntervalSeconds.Should().Be(300);
        monitor.IsEnabled.Should().BeFalse();
        monitor.UpdatedAt.Should().BeOnOrAfter(monitor.CreatedAt);
    }
}

