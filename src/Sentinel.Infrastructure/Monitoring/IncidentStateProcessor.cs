using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sentinel.Application.Monitoring;
using Sentinel.Domain.Checks;
using Sentinel.Domain.Incidents;
using Sentinel.Domain.Monitors;
using Sentinel.Domain.Notifications;
using Sentinel.Infrastructure.Persistence;
using DomainMonitor = Sentinel.Domain.Monitors.Monitor;

namespace Sentinel.Infrastructure.Monitoring;

public sealed class IncidentStateProcessor(
    ApplicationDbContext dbContext,
    IOptions<MonitoringOptions> options)
{
    private readonly MonitoringOptions monitoringOptions = options.Value;

    public async Task ApplyAsync(
        DomainMonitor monitor,
        CheckResult checkResult,
        CancellationToken cancellationToken)
    {
        monitor.RecordCheck(checkResult.IsHealthy);

        var openIncident = await dbContext.Incidents
            .SingleOrDefaultAsync(
                incident => incident.MonitorId == monitor.Id && incident.Status == IncidentStatuses.Open,
                cancellationToken);

        if (!checkResult.IsHealthy &&
            openIncident is null &&
            monitor.ConsecutiveFailureCount >= monitoringOptions.IncidentFailureThreshold)
        {
            var incident = Incident.Open(monitor.Id, checkResult.CheckedAt);
            var transitionId = Guid.NewGuid();
            dbContext.Incidents.Add(incident);
            dbContext.IncidentTransitions.Add(new IncidentTransition(
                transitionId,
                incident.Id,
                monitor.Id,
                IncidentTransitionTypes.Opened,
                checkResult.CheckedAt,
                checkResult.Id));
            dbContext.NotificationOutbox.Add(new NotificationOutbox(
                transitionId,
                incident.Id,
                monitor.Id,
                NotificationEventTypes.IncidentOpened,
                checkResult.CheckedAt));
        }
        else if (checkResult.IsHealthy &&
                 openIncident is not null &&
                 monitor.ConsecutiveSuccessCount >= monitoringOptions.IncidentRecoveryThreshold)
        {
            openIncident.Recover(checkResult.CheckedAt);
            var transitionId = Guid.NewGuid();
            dbContext.IncidentTransitions.Add(new IncidentTransition(
                transitionId,
                openIncident.Id,
                monitor.Id,
                IncidentTransitionTypes.Recovered,
                checkResult.CheckedAt,
                checkResult.Id));
            dbContext.NotificationOutbox.Add(new NotificationOutbox(
                transitionId,
                openIncident.Id,
                monitor.Id,
                NotificationEventTypes.IncidentRecovered,
                checkResult.CheckedAt));
        }
    }
}
