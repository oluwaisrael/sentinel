using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Sentinel.IntegrationTests.Infrastructure;

namespace Sentinel.IntegrationTests;

public sealed class ApiFactory(PostgresFixture postgresFixture) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", postgresFixture.ConnectionString);
        builder.UseSetting("Jwt:Issuer", "sentinel-test");
        builder.UseSetting("Jwt:Audience", "sentinel-test-client");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-that-is-at-least-32-chars");
        builder.UseSetting("Jwt:AccessTokenMinutes", "30");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = postgresFixture.ConnectionString,
                ["Jwt:Issuer"] = "sentinel-test",
                ["Jwt:Audience"] = "sentinel-test-client",
                ["Jwt:SigningKey"] = "integration-test-signing-key-that-is-at-least-32-chars",
                ["Jwt:AccessTokenMinutes"] = "30",
                ["Database:ApplyMigrations"] = "true"
            });
        });
    }
}

