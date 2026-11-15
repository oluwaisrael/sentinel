using FluentAssertions;
using Sentinel.Domain.Common;
using Sentinel.Domain.Monitors;
using DomainMonitor = Sentinel.Domain.Monitors.Monitor;

namespace Sentinel.UnitTests.Monitors;

public sealed class MonitorTests
{
    [Fact]
    public void Create_with_valid_values_creates_enabled_monitor()
    {
        var monitor = DomainMonitor.Create("user-1", "Public API", "https://api.example.com/health", 60, 5_000);

        monitor.OwnerId.Should().Be("user-1");
        monitor.Name.Should().Be("Public API");
        monitor.IsEnabled.Should().BeTrue();
        monitor.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_trims_display_name_and_url()
    {
        var monitor = DomainMonitor.Create("user-1", "  Public API  ", "  https://api.example.com  ", 60, 5_000);

        monitor.Name.Should().Be("Public API");
        monitor.Url.Should().Be("https://api.example.com");
    }

    [Theory]
    [InlineData(29)]
    [InlineData(86_401)]
    public void Create_rejects_interval_outside_supported_range(int intervalSeconds)
    {
        var action = () => DomainMonitor.Create("user-1", "API", "https://api.example.com", intervalSeconds, 5_000);

        action.Should().Throw<DomainException>().WithMessage("*intervalSeconds*");
    }

    [Fact]
    public void Create_rejects_blank_owner()
    {
        var action = () => DomainMonitor.Create(" ", "API", "https://api.example.com", 60, 5_000);

        action.Should().Throw<DomainException>().WithMessage("*ownerId*");
    }

    [Fact]
    public void Create_rejects_timeout_below_minimum()
    {
        var action = () => DomainMonitor.Create("user-1", "API", "https://api.example.com", 60, 99);

        action.Should().Throw<DomainException>().WithMessage("*timeoutMilliseconds*");
    }

    [Fact]
    public void Create_rejects_timeout_above_maximum()
    {
        var action = () => DomainMonitor.Create("user-1", "API", "https://api.example.com", 60, 120_001);

        action.Should().Throw<DomainException>().WithMessage("*timeoutMilliseconds*");
    }

    [Fact]
    public void Create_accepts_minimum_interval_boundary()
    {
        var monitor = DomainMonitor.Create("user-1", "API", "https://api.example.com", 30, 100);

        monitor.IntervalSeconds.Should().Be(30);
        monitor.TimeoutMilliseconds.Should().Be(100);
    }

    [Fact]
    public void Create_rejects_blank_name()
    {
        var action = () => DomainMonitor.Create("user-1", " ", "https://api.example.com", 60, 5_000);

        action.Should().Throw<DomainException>().WithMessage("*name*");
    }

    [Fact]
    public void Update_changes_configuration_and_state()
    {
        var monitor = DomainMonitor.Create("user-1", "API", "https://api.example.com", 60, 5_000);

        monitor.Update("Updated API", "https://status.example.com", 300, 10_000, false);

        monitor.Name.Should().Be("Updated API");
        monitor.Url.Should().Be("https://status.example.com");
        monitor.IntervalSeconds.Should().Be(300);
        monitor.IsEnabled.Should().BeFalse();
        monitor.UpdatedAt.Should().BeOnOrAfter(monitor.CreatedAt);
    }
}

