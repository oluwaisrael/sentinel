using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Sentinel.Infrastructure.Persistence;

public sealed class DatabaseInitializer(
    ApplicationDbContext dbContext,
    ILogger<DatabaseInitializer> logger)
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Applying database migrations");
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}

