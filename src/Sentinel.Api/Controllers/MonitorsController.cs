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
        var monitor = await ExecuteAsync(() => monitorService.CreateAsync(CurrentUserId(), request, cancellationToken));
        return monitor.Value is not null
            ? CreatedAtAction(nameof(Get), new { id = monitor.Value.Id }, monitor.Value)
            : monitor;
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
    }
}

