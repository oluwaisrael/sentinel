using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sentinel.Domain.Monitors;
using Sentinel.Domain.Checks;
using Sentinel.Infrastructure.Identity;
using DomainMonitor = Sentinel.Domain.Monitors.Monitor;

namespace Sentinel.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<DomainMonitor> Monitors => Set<DomainMonitor>();

    public DbSet<CheckResult> CheckResults => Set<CheckResult>();

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
    }
}

