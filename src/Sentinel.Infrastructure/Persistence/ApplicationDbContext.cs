using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sentinel.Domain.Monitors;
using Sentinel.Infrastructure.Identity;

namespace Sentinel.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Monitor> Monitors => Set<Monitor>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Monitor>(entity =>
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
            entity.HasIndex(monitor => new { monitor.OwnerId, monitor.Name }).IsUnique();
            entity.HasIndex(monitor => new { monitor.IsEnabled, monitor.UpdatedAt });
        });
    }
}

