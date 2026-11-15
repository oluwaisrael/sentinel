using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using Sentinel.Application.Monitoring;
using Sentinel.Application.Monitors;

namespace Sentinel.Infrastructure.Monitoring;

public sealed class HttpMonitorChecker(IHttpClientFactory httpClientFactory) : IMonitorChecker
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    public async Task<MonitorCheckOutcome> CheckAsync(
        MonitorLease lease,
        CancellationToken cancellationToken)
    {
        var checkedAt = DateTimeOffset.UtcNow;
        if (!Uri.TryCreate(lease.Url, UriKind.Absolute, out var uri) || !MonitorUrlPolicy.IsAllowed(uri))
        {
            return new MonitorCheckOutcome(checkedAt, null, null, false, CheckErrorTypes.BlockedDestination);
        }

        try
        {
            var publicAddresses = await MonitorUrlPolicy.ResolvePublicAddressesAsync(uri.Host, cancellationToken);
            if (publicAddresses.Count == 0)
            {
                return new MonitorCheckOutcome(checkedAt, null, null, false, CheckErrorTypes.BlockedDestination);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new MonitorCheckOutcome(DateTimeOffset.UtcNow, null, null, false, CheckErrorTypes.Timeout);
        }
        catch (SocketException)
        {
            return new MonitorCheckOutcome(DateTimeOffset.UtcNow, null, null, false, CheckErrorTypes.DnsFailure);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(RequestTimeout);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await httpClientFactory
                .CreateClient("monitor-checks")
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
            stopwatch.Stop();

            var statusCode = (int)response.StatusCode;
            return new MonitorCheckOutcome(
                DateTimeOffset.UtcNow,
                statusCode,
                (int)stopwatch.ElapsedMilliseconds,
                statusCode is >= 200 and <= 299,
                CheckErrorTypes.None);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new MonitorCheckOutcome(DateTimeOffset.UtcNow, null, (int)stopwatch.ElapsedMilliseconds, false, CheckErrorTypes.Cancelled);
        }
        catch (OperationCanceledException)
        {
            return new MonitorCheckOutcome(DateTimeOffset.UtcNow, null, (int)stopwatch.ElapsedMilliseconds, false, CheckErrorTypes.Timeout);
        }
        catch (HttpRequestException)
        {
            return new MonitorCheckOutcome(DateTimeOffset.UtcNow, null, (int)stopwatch.ElapsedMilliseconds, false, CheckErrorTypes.ConnectionFailure);
        }
        catch
        {
            return new MonitorCheckOutcome(DateTimeOffset.UtcNow, null, (int)stopwatch.ElapsedMilliseconds, false, CheckErrorTypes.Unexpected);
        }
    }
}

