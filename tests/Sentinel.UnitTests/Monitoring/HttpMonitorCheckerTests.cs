using System.Net;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Http;
using Sentinel.Application.Monitoring;
using Sentinel.Infrastructure.Monitoring;

namespace Sentinel.UnitTests.Monitoring;

public sealed class HttpMonitorCheckerTests
{
    [Fact]
    public async Task Returns_healthy_for_successful_http_response()
    {
        var checker = CreateChecker(new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));

        var result = await checker.CheckAsync(CreateLease(), CancellationToken.None);

        result.IsHealthy.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.ErrorType.Should().Be(CheckErrorTypes.None);
        result.LatencyMs.Should().NotBeNull();
    }

    [Fact]
    public async Task Returns_unhealthy_for_non_success_http_response()
    {
        var checker = CreateChecker(new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));

        var result = await checker.CheckAsync(CreateLease(), CancellationToken.None);

        result.IsHealthy.Should().BeFalse();
        result.StatusCode.Should().Be(503);
        result.ErrorType.Should().Be(CheckErrorTypes.None);
    }

    [Fact]
    public async Task Blocks_literal_private_destination_before_http_request()
    {
        var handlerCalled = false;
        var checker = CreateChecker(new StubHandler(_ =>
        {
            handlerCalled = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }));

        var result = await checker.CheckAsync(CreateLease("http://127.0.0.1/health"), CancellationToken.None);

        result.ErrorType.Should().Be(CheckErrorTypes.BlockedDestination);
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Classifies_cancellation_separately_from_timeout()
    {
        var checker = CreateChecker(new StubHandler(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await checker.CheckAsync(CreateLease(), cancellation.Token);

        result.ErrorType.Should().Be(CheckErrorTypes.Cancelled);
    }

    [Fact]
    public async Task Enforces_timeout_before_five_second_upper_bound()
    {
        var checker = CreateChecker(new StubHandler(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));

        var result = await checker.CheckAsync(CreateLease(timeoutMilliseconds: 50), CancellationToken.None);

        result.ErrorType.Should().Be(CheckErrorTypes.Timeout);
        result.IsHealthy.Should().BeFalse();
    }

    private static HttpMonitorChecker CreateChecker(HttpMessageHandler handler) =>
        new(new StubHttpClientFactory(handler));

    private static MonitorLease CreateLease(
        string url = "http://203.0.113.10/health",
        int timeoutMilliseconds = 5_000) =>
        new(Guid.NewGuid(), url, 60, timeoutMilliseconds, Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(30));

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler(
        Func<CancellationToken, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => callback(cancellationToken);
    }
}

