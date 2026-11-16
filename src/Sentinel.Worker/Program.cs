using Sentinel.Worker;
using Sentinel.Infrastructure;
using Sentinel.Application.Monitoring;
using Sentinel.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<MonitoringBackgroundService>();
builder.Services.AddHostedService<NotificationBackgroundService>();

var host = builder.Build();
if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = host.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().MigrateAsync(host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping);
}

host.Run();
