using Microsoft.Extensions.Logging;

namespace Sentinel.Infrastructure.Persistence;

internal static partial class DatabaseLog
{
    [LoggerMessage(EventId = 1101, Level = LogLevel.Information, Message = "Applying database migrations")]
    public static partial void ApplyingMigrations(ILogger logger);
}
