using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Identity;
using Sentinel.Application.Monitoring;
using Sentinel.Application.Monitors;
using Sentinel.Domain.Checks;
using Sentinel.Domain.Monitors;
using Sentinel.Infrastructure.Persistence;
using Sentinel.IntegrationTests.Infrastructure;
using DomainMonitor = Sentinel.Domain.Monitors.Monitor;

namespace Sentinel.IntegrationTests;

public sealed class MonitoringPersistenceTests(PostgresFixture postgresFixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Concurrent_claims_allow_only_one_worker_and_expired_lease_is_recoverable()
    {
        await using var factory = new ApiFactory(postgresFixture);
        var monitorId = await SeedMonitorAsync(factory);
        var claimTime = DateTimeOffset.UtcNow.AddSeconds(1);

        await using var firstScope = factory.Services.CreateAsyncScope();
        await using var secondScope = factory.Services.CreateAsyncScope();
        var firstStore = firstScope.ServiceProvider.GetRequiredService<IMonitorWorkStore>();
        var secondStore = secondScope.ServiceProvider.GetRequiredService<IMonitorWorkStore>();
        var claims = await Task.WhenAll(
            firstStore.TryClaimDueAsync(claimTime, TimeSpan.FromSeconds(30), CancellationToken.None),
            secondStore.TryClaimDueAsync(claimTime, TimeSpan.FromSeconds(30), CancellationToken.None));

        claims.Count(claim => claim is not null).Should().Be(1);
        var firstLease = claims.Single(claim => claim is not null)!;
        var recoveredLease = await firstStore.TryClaimDueAsync(
            claimTime.AddSeconds(31),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);
        recoveredLease.Should().NotBeNull();

        var staleRecorded = await firstStore.RecordCheckAsync(
            firstLease,
            new MonitorCheckOutcome(DateTimeOffset.UtcNow, 200, 120, true, CheckErrorTypes.None),
            claimTime.AddMinutes(1),
            CancellationToken.None);
        staleRecorded.Should().BeFalse();

        var activeRecorded = await firstStore.RecordCheckAsync(
            recoveredLease!,
            new MonitorCheckOutcome(DateTimeOffset.UtcNow, 503, 240, false, CheckErrorTypes.None),
            claimTime.AddMinutes(2),
            CancellationToken.None);
        activeRecorded.Should().BeTrue();

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await db.CheckResults.CountAsync(result => result.MonitorId == monitorId)).Should().Be(1);
    }

    [Fact]
    public async Task Checks_and_stats_are_bounded_and_owned()
    {
        await using var factory = new ApiFactory(postgresFixture);
        using var client = factory.CreateClient();
        var email = $"history-{Guid.NewGuid():N}@example.com";
        const string password = "Strong-password-123!";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = password });
        var auth = await AuthenticateAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var create = await client.PostAsJsonAsync("/api/monitors", new CreateMonitorRequest
        {
            Name = "History API",
            Url = "https://api.example.com/health",
            IntervalSeconds = 60
        });
        var monitor = await create.Content.ReadFromJsonAsync<MonitorResponse>();
        var now = DateTimeOffset.UtcNow;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.CheckResults.AddRange(
                new CheckResult(Guid.NewGuid(), monitor!.Id, now.AddMinutes(-2), 200, 100, true, CheckErrorTypes.None),
                new CheckResult(Guid.NewGuid(), monitor.Id, now.AddMinutes(-1), 503, 200, false, CheckErrorTypes.None));
            await db.SaveChangesAsync();
        }

        var checks = await client.GetFromJsonAsync<List<MonitorCheckResponse>>(
            $"/api/monitors/{monitor!.Id}/checks?limit=1");
        var stats = await client.GetFromJsonAsync<MonitorStatsResponse>(
            $"/api/monitors/{monitor.Id}/stats?from={Uri.EscapeDataString(now.AddDays(-1).ToString("O"))}&to={Uri.EscapeDataString(now.ToString("O"))}");

        checks.Should().ContainSingle();
        stats!.TotalChecks.Should().Be(2);
        stats.HealthyChecks.Should().Be(1);
        stats.UptimePercentage.Should().Be(50);
        stats.AverageLatencyMs.Should().Be(150);
    }

    [Fact]
    public async Task Checks_endpoint_hides_another_users_history()
    {
        await using var factory = new ApiFactory(postgresFixture);
        using var client = factory.CreateClient();
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        const string password = "Strong-password-123!";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = password });
        var auth = await AuthenticateAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await client.GetAsync($"/api/monitors/{Guid.NewGuid()}/checks");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<Guid> SeedMonitorAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var monitor = DomainMonitor.Create("scheduler-owner", $"Scheduler-{Guid.NewGuid():N}", "https://api.example.com", 60, 5_000);
        db.Monitors.Add(monitor);
        await db.SaveChangesAsync();
        return monitor.Id;
    }

    private static async Task<AuthResponse> AuthenticateAsync(HttpClient client, string email, string password)
    {
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
        return (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
}

