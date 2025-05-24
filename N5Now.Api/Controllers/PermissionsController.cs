using MediatR;
using Microsoft.AspNetCore.Mvc;
using N5Now.Application.Commands.RequestPermission;
using N5Now.Application.Commands.ModifyPermission;
using N5Now.Application.Queries.GetPermissions;

namespace N5Now.Api.Controllers;

[ApiController]
[Route("permissions")]
public class PermissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PermissionsController> _logger;


    public PermissionsController(IMediator mediator,ILogger<PermissionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // POST /permissions/request
    [HttpPost("request")]
    public async Task<IActionResult> RequestPermission([FromBody] RequestPermissionCommand command)
    {
        _logger.LogInformation("Requesting permission for {EmployeeName} on {Date}", command.EmployeeName, command.PermissionDate);
        var id = await _mediator.Send(command);
        return Ok(new { PermissionId = id });
    }

    // PUT /permissions/modify
    [HttpPut("modify")]
    public async Task<IActionResult> ModifyPermission([FromBody] ModifyPermissionCommand command)
    {
        _logger.LogInformation("Modifying permission {Id}", command.Id);

        var success = await _mediator.Send(command);
        if (!success)
        {
            _logger.LogWarning("Permission {Id} not found for modification", command.Id);

            return NotFound(new { Message = "Permission not found." });
        }
        _logger.LogInformation("Permission {Id} modified successfully", command.Id);

        return Ok(new { Message = "Permission modified." });
    }

    [HttpGet]
    public async Task<IActionResult> GetPermissions()
    {
        _logger.LogInformation("Getting list of permissions");
        var result = await _mediator.Send(new GetPermissionsQuery());
        _logger.LogInformation("Returned {Count} permissions", result.Count());
        return Ok(result);
    }
}
