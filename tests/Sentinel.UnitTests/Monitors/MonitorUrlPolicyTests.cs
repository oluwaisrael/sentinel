using FluentAssertions;
using Sentinel.Application.Monitors;

namespace Sentinel.UnitTests.Monitors;

public sealed class MonitorUrlPolicyTests
{
    [Theory]
    [InlineData("https://api.example.com/health")]
    [InlineData("http://203.0.113.10/status")]
    public void Allows_public_http_urls(string value)
    {
        MonitorUrlPolicy.IsAllowed(new Uri(value)).Should().BeTrue();
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("http://localhost:8080/health")]
    [InlineData("http://127.0.0.1:8080/health")]
    [InlineData("http://10.0.0.4/health")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("https://user:password@example.com/health")]
    public void Rejects_unsafe_monitor_urls(string value)
    {
        MonitorUrlPolicy.IsAllowed(new Uri(value)).Should().BeFalse();
    }

    [Theory]
    [InlineData("http://[::1]/health")]
    [InlineData("http://[fc00::1]/health")]
    [InlineData("http://[fe80::1]/health")]
    public void Rejects_local_ipv6_urls(string value)
    {
        MonitorUrlPolicy.IsAllowed(new Uri(value)).Should().BeFalse();
    }
}

