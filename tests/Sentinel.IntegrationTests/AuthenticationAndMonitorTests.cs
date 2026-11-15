using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Sentinel.Application.Identity;
using Sentinel.Application.Monitors;
using Sentinel.IntegrationTests.Infrastructure;

namespace Sentinel.IntegrationTests;

public sealed class AuthenticationAndMonitorTests(PostgresFixture postgresFixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task User_can_register_login_and_manage_owned_monitor()
    {
        await using var factory = new ApiFactory(postgresFixture);
        using var client = factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registration = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Strong-password-123!"
        });
        registration.StatusCode.Should().Be(HttpStatusCode.Created, await registration.Content.ReadAsStringAsync());

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Strong-password-123!"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var create = await client.PostAsJsonAsync(
            "/api/monitors",
            new CreateMonitorRequest
            {
                Name = "Example API",
                Url = "https://api.example.com/health",
                IntervalSeconds = 60,
                TimeoutMilliseconds = 5_000
            });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var monitor = await create.Content.ReadFromJsonAsync<MonitorResponse>();
        monitor.Should().NotBeNull();

        var list = await client.GetFromJsonAsync<List<MonitorResponse>>("/api/monitors");
        list.Should().ContainSingle(item => item.Id == monitor!.Id);

        var delete = await client.DeleteAsync($"/api/monitors/{monitor!.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Monitor_endpoints_require_authentication()
    {
        await using var factory = new ApiFactory(postgresFixture);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/monitors");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_unauthorized()
    {
        await using var factory = new ApiFactory(postgresFixture);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "missing@example.com",
            Password = "Wrong-password-123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

