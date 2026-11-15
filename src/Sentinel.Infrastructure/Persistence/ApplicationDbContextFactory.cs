using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sentinel.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            Environment.GetEnvironmentVariable("SENTINEL_CONNECTION_STRING") ??
            "Host=localhost;Port=5432;Database=sentinel;Username=sentinel;Password=sentinel-dev-only");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

