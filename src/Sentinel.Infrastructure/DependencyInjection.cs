using System.Net.Sockets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sentinel.Application.Identity;
using Sentinel.Application.Monitoring;
using Sentinel.Application.Monitors;
using Sentinel.Infrastructure.Identity;
using Sentinel.Infrastructure.Monitors;
using Sentinel.Infrastructure.Monitoring;
using Sentinel.Infrastructure.Persistence;

namespace Sentinel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddOptions<MonitoringOptions>()
            .Bind(configuration.GetSection(MonitoringOptions.SectionName))
            .Validate(options => options.PollIntervalMilliseconds is >= 100 and <= 60_000, "Monitoring poll interval must be between 100ms and 60s.")
            .Validate(options => options.MaxConcurrency is >= 1 and <= 32, "Monitoring concurrency must be between 1 and 32.")
            .Validate(options => options.LeaseDurationSeconds is >= 10 and <= 300, "Monitoring lease must be between 10s and 5m.")
            .Validate(options => options.SupportedIntervalSeconds == 60, "Only 60-second monitoring intervals are currently supported.")
            .ValidateOnStart();
        services.AddHttpClient("monitor-checks", client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SentinelMonitor/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                ConnectCallback = async (context, cancellationToken) =>
                {
                    var addresses = await MonitorUrlPolicy.ResolvePublicAddressesAsync(
                        context.DnsEndPoint.Host,
                        cancellationToken);
                    if (addresses.Count == 0)
                    {
                        throw new HttpRequestException("The monitor destination resolved to a restricted address.");
                    }

                    foreach (var address in addresses)
                    {
                        var tcpClient = new TcpClient(address.AddressFamily);
                        try
                        {
                            await tcpClient.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken);
                            return tcpClient.GetStream();
                        }
                        catch
                        {
                            tcpClient.Dispose();
                        }
                    }

                    throw new HttpRequestException("The monitor destination could not be connected.");
                }
            });
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "JWT issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "JWT audience is required.")
            .Validate(options => options.SigningKey.Length >= 32, "JWT signing key must be at least 32 characters.")
            .Validate(options => options.AccessTokenMinutes is > 0 and <= 1_440, "JWT lifetime must be between 1 minute and 24 hours.")
            .ValidateOnStart();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddSignInManager<SignInManager<ApplicationUser>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMonitorService, MonitorService>();
        services.AddScoped<IMonitorWorkStore, MonitorWorkStore>();
        services.AddSingleton<IMonitorChecker, HttpMonitorChecker>();
        services.AddScoped<DatabaseInitializer>();
        return services;
    }
}

