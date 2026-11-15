using Sentinel.Worker;
using Sentinel.Infrastructure;
using Sentinel.Application.Monitoring;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<MonitoringBackgroundService>();

var host = builder.Build();
host.Run();
