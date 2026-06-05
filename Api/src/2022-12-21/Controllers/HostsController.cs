namespace BuberDinner._2022_12_21.Controllers;

using System.Security.Claims;
using Asp.Versioning;
using BuberDinner.Api._2022_12_21.Models.Hosts;
using BuberDinner.Domain.User.ValueObjects;
using Mapster;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Trellis.Asp;

/// <summary>
/// Register and manage Hosts. A Host is owned by the authenticated user;
/// the owner relationship gates resource-based authorization on menu mutations.
/// </summary>
[ApiVersion("2022-10-01")]
[Route("hosts")]
[Consumes("application/json")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status201Created)]
public class HostsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of <see cref="HostsController"/>.</summary>
    /// <param name="sender">The Mediator <see cref="ISender"/> used to dispatch commands.</param>
    public HostsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Register a new Host owned by the authenticated user. The OwnerId is read from the JWT `sub`
    /// claim (the same claim Trellis ClaimsActorProvider uses for Actor.Id).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<HostResponse>> CreateHost(
        [FromBody] CreateHostRequest request,
        CancellationToken cancellationToken)
    {
        var ownerIdString = HttpContext.User.FindFirst("sub")?.Value
            ?? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(ownerIdString))
            return Unauthorized();

        var ownerIdResult = UserId.TryCreate(ownerIdString);
        if (ownerIdResult.IsFailure)
            return Unauthorized();
        var ownerId = ownerIdResult.GetValueOrThrow(nameof(ownerIdString));

        return await request.ToCreateHostCommand(ownerId)
            .BindAsync(command => _sender.Send(command, cancellationToken))
            .ToHttpResponseAsync(
                body: host => new HostResponse(host.Id.Value.ToString(), host.OwnerId.Value, host.DisplayName.Value),
                configure: opts => opts.Created(host => $"/hosts/{host.Id.Value}"))
            .AsActionResultAsync<HostResponse>();
    }
}
