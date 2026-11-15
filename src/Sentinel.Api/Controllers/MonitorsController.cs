using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentinel.Application.Common;
using Sentinel.Application.Monitors;
using Sentinel.Domain.Common;

namespace Sentinel.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/monitors")]
public sealed class MonitorsController(IMonitorService monitorService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MonitorResponse>>> List(CancellationToken cancellationToken)
        => Ok(await monitorService.ListAsync(CurrentUserId(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MonitorResponse>> Get(Guid id, CancellationToken cancellationToken)
        => await ExecuteAsync(() => monitorService.GetAsync(CurrentUserId(), id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<MonitorResponse>> Create(CreateMonitorRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var monitor = await monitorService.CreateAsync(CurrentUserId(), request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = monitor.Id }, monitor);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (DomainException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MonitorResponse>> Update(
        Guid id,
        UpdateMonitorRequest request,
        CancellationToken cancellationToken)
        => await ExecuteAsync(() => monitorService.UpdateAsync(CurrentUserId(), id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await monitorService.DeleteAsync(CurrentUserId(), id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id:guid}/checks")]
    public async Task<ActionResult<IReadOnlyList<MonitorCheckResponse>>> Checks(
        Guid id,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 500)
        {
            return BadRequest(new { error = "limit must be between 1 and 500." });
        }

        var windowEnd = to ?? DateTimeOffset.UtcNow;
        var windowStart = from ?? windowEnd.AddDays(-7);
        if (windowStart > windowEnd || windowEnd - windowStart > TimeSpan.FromDays(31))
        {
            return BadRequest(new { error = "The check history window must be valid and no longer than 31 days." });
        }

        try
        {
            return Ok(await monitorService.GetChecksAsync(
                CurrentUserId(), id, windowStart, windowEnd, limit, cancellationToken));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id:guid}/stats")]
    public async Task<ActionResult<MonitorStatsResponse>> Stats(
        Guid id,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var windowEnd = to ?? DateTimeOffset.UtcNow;
        var windowStart = from ?? windowEnd.AddDays(-7);
        if (windowStart > windowEnd || windowEnd - windowStart > TimeSpan.FromDays(31))
        {
            return BadRequest(new { error = "The statistics window must be valid and no longer than 31 days." });
        }

        try
        {
            return Ok(await monitorService.GetStatsAsync(
                CurrentUserId(), id, windowStart, windowEnd, cancellationToken));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    private string CurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("sub") ??
        throw new InvalidOperationException("Authenticated user identifier is missing.");

    private async Task<ActionResult<MonitorResponse>> ExecuteAsync(Func<Task<MonitorResponse>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (DomainException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }
}

