using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sentinel.Domain.Monitors;
using Sentinel.Domain.Checks;
using Sentinel.Domain.Incidents;
using Sentinel.Domain.Notifications;
using Sentinel.Infrastructure.Identity;
using DomainMonitor = Sentinel.Domain.Monitors.Monitor;

namespace Sentinel.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<DomainMonitor> Monitors => Set<DomainMonitor>();

    public DbSet<CheckResult> CheckResults => Set<CheckResult>();

    public DbSet<Incident> Incidents => Set<Incident>();

    public DbSet<IncidentTransition> IncidentTransitions => Set<IncidentTransition>();

    public DbSet<NotificationOutbox> NotificationOutbox => Set<NotificationOutbox>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<DomainMonitor>(entity =>
        {
            entity.ToTable("monitors");
            entity.HasKey(monitor => monitor.Id);
            entity.Property(monitor => monitor.OwnerId).HasMaxLength(450).IsRequired();
            entity.Property(monitor => monitor.Name).HasMaxLength(100).IsRequired();
            entity.Property(monitor => monitor.Url).HasMaxLength(2048).IsRequired();
            entity.Property(monitor => monitor.IntervalSeconds).IsRequired();
            entity.Property(monitor => monitor.TimeoutMilliseconds).IsRequired();
            entity.Property(monitor => monitor.IsEnabled).IsRequired();
            entity.Property(monitor => monitor.CreatedAt).IsRequired();
            entity.Property(monitor => monitor.UpdatedAt).IsRequired();
            entity.Property(monitor => monitor.NextCheckAt).IsRequired();
            entity.Property(monitor => monitor.LeaseId);
            entity.Property(monitor => monitor.LeaseExpiresAt);
            entity.Property(monitor => monitor.ConsecutiveFailureCount).IsRequired();
            entity.Property(monitor => monitor.ConsecutiveSuccessCount).IsRequired();
            entity.HasIndex(monitor => new { monitor.OwnerId, monitor.Name }).IsUnique();
            entity.HasIndex(monitor => new { monitor.IsEnabled, monitor.UpdatedAt });
            entity.HasIndex(monitor => new { monitor.IsEnabled, monitor.NextCheckAt, monitor.LeaseExpiresAt });
        });

        builder.Entity<CheckResult>(entity =>
        {
            entity.ToTable("check_results");
            entity.HasKey(result => result.Id);
            entity.Property(result => result.ErrorType).HasMaxLength(64).IsRequired();
            entity.Property(result => result.CheckedAt).IsRequired();
            entity.Property(result => result.IsHealthy).IsRequired();
            entity.HasIndex(result => new { result.MonitorId, result.CheckedAt });
            entity.HasOne<DomainMonitor>()
                .WithMany()
                .HasForeignKey(result => result.MonitorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Incident>(entity =>
        {
            entity.ToTable("incidents");
            entity.HasKey(incident => incident.Id);
            entity.Property(incident => incident.Status).HasMaxLength(32).IsRequired();
            entity.Property(incident => incident.OpenedAt).IsRequired();
            entity.Property(incident => incident.LastTransitionAt).IsRequired();
            entity.HasIndex(incident => incident.MonitorId);
            entity.HasIndex(incident => incident.MonitorId)
                .HasFilter("\"Status\" = 'Open'")
                .IsUnique();
            entity.HasOne<DomainMonitor>()
                .WithMany()
                .HasForeignKey(incident => incident.MonitorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IncidentTransition>(entity =>
        {
            entity.ToTable("incident_transitions");
            entity.HasKey(transition => transition.Id);
            entity.Property(transition => transition.TransitionType).HasMaxLength(32).IsRequired();
            entity.HasIndex(transition => new { transition.IncidentId, transition.TransitionType }).IsUnique();
            entity.HasIndex(transition => new { transition.MonitorId, transition.OccurredAt });
            entity.HasOne<Incident>()
                .WithMany()
                .HasForeignKey(transition => transition.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<CheckResult>()
                .WithMany()
                .HasForeignKey(transition => transition.CheckResultId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<NotificationOutbox>(entity =>
        {
            entity.ToTable("notification_outbox");
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.EventType).HasMaxLength(64).IsRequired();
            entity.Property(notification => notification.Status).HasMaxLength(32).IsRequired();
            entity.Property(notification => notification.LastError).HasMaxLength(2_000);
            entity.HasIndex(notification => new { notification.Status, notification.NextAttemptAt, notification.LeaseExpiresAt });
            entity.HasIndex(notification => new { notification.IncidentId, notification.EventType }).IsUnique();
            entity.HasOne<Incident>()
                .WithMany()
                .HasForeignKey(notification => notification.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

