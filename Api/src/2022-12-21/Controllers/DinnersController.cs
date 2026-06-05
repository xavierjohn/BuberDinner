namespace BuberDinner._2022_12_21.Controllers;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using BuberDinner.Api._2022_12_21.Models.Dinners;
using BuberDinner.Application.Dinners.Commands;
using BuberDinner.Application.Dinners.Queries;
using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Host.ValueObject;
using Mapster;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Trellis;
using Trellis.Asp;

/// <summary>
/// CRUD + lifecycle endpoints for the <see cref="Dinner"/> aggregate. State-transition POSTs
/// (<c>/start</c>, <c>/end</c>, <c>/cancel</c>) are guarded by the aggregate's Stateless
/// state machine — Cookbook Recipe 23 explicitly notes these endpoints do NOT need
/// <c>If-Match</c>; an invalid transition is a semantic rule violation (422), not a
/// concurrent-modification conflict (412).
/// </summary>
[ApiVersion("2022-10-01")]
[Route("hosts/{hostId:HostId}/dinners")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status200OK)]
public class DinnersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initialises a new <see cref="DinnersController"/> with the supplied Mediator sender.</summary>
    public DinnersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Schedules a new dinner under the route host. The handler validates that the supplied
    /// MenuId belongs to the same host (404 otherwise) and that EndDateTime &gt; StartDateTime
    /// (422 otherwise). Resource auth (<see cref="Trellis.Authorization.IAuthorizeResource{T}"/>)
    /// gates by Host ownership.
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<DinnerResponse>> ScheduleDinner(
        HostId hostId,
        [FromBody] ScheduleDinnerRequest request,
        CancellationToken cancellationToken) =>
        await request.ToScheduleDinnerCommand(hostId)
            .BindAsync(command => _sender.Send(command, cancellationToken))
            .ToHttpResponseAsync(
                body: dinner => dinner.Adapt<DinnerResponse>(),
                configure: opts => opts
                    .Created(dinner => $"/hosts/{hostId.Value}/dinners/{dinner.Id.Value}")
                    .WithETag(dinner => EntityTagValue.Strong(dinner.ETag)))
            .AsActionResultAsync<DinnerResponse>();

    /// <summary>
    /// Lists every dinner owned by the route host. PR 3 will replace this with paginated
    /// <see cref="Trellis.Page{T}"/> + <see cref="Trellis.Cursor"/> output.
    /// </summary>
    [HttpGet]
    public async ValueTask<ActionResult<DinnerResponse[]>> ListDinners(
        HostId hostId, CancellationToken cancellationToken) =>
        await _sender.Send(new ListDinnersForHostQuery(hostId), cancellationToken)
            .ToHttpResponseAsync(
                body: dinners => dinners.Select(d => d.Adapt<DinnerResponse>()).ToArray())
            .AsActionResultAsync<DinnerResponse[]>();

    /// <summary>
    /// Get a dinner by id. Emits a strong ETag (Cookbook Recipe 6) — though dinners are
    /// mutated only through the state-machine POSTs, the ETag still lets clients revalidate
    /// cached reads with <c>If-None-Match</c> (304).
    /// </summary>
    [HttpGet("{dinnerId:DinnerId}")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<DinnerResponse>> GetDinner(
        HostId hostId, DinnerId dinnerId, CancellationToken cancellationToken) =>
        await _sender.Send(new GetDinnerQuery(hostId, dinnerId), cancellationToken)
            .ToHttpResponseAsync(
                body: dinner => dinner.Adapt<DinnerResponse>(),
                configure: opts => opts
                    .WithETag(dinner => EntityTagValue.Strong(dinner.ETag))
                    .EvaluatePreconditions())
            .AsActionResultAsync<DinnerResponse>();

    /// <summary>
    /// Transitions the dinner to <c>InProgress</c>. Body-less per Cookbook Recipe 23.
    /// Invalid transition (e.g. dinner already started or ended) → 422 with reason code
    /// <c>state.machine.invalid.transition</c>.
    /// </summary>
    [HttpPost("{dinnerId:DinnerId}/start")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<DinnerResponse>> StartDinner(
        HostId hostId, DinnerId dinnerId, CancellationToken cancellationToken) =>
        await _sender.Send(new StartDinnerCommand(hostId, dinnerId), cancellationToken)
            .ToHttpResponseAsync(
                body: dinner => dinner.Adapt<DinnerResponse>(),
                configure: opts => opts.WithETag(dinner => EntityTagValue.Strong(dinner.ETag)))
            .AsActionResultAsync<DinnerResponse>();

    /// <summary>
    /// Transitions the dinner to <c>Ended</c>. Body-less.
    /// </summary>
    [HttpPost("{dinnerId:DinnerId}/end")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<DinnerResponse>> EndDinner(
        HostId hostId, DinnerId dinnerId, CancellationToken cancellationToken) =>
        await _sender.Send(new EndDinnerCommand(hostId, dinnerId), cancellationToken)
            .ToHttpResponseAsync(
                body: dinner => dinner.Adapt<DinnerResponse>(),
                configure: opts => opts.WithETag(dinner => EntityTagValue.Strong(dinner.ETag)))
            .AsActionResultAsync<DinnerResponse>();

    /// <summary>
    /// Transitions the dinner to <c>Cancelled</c>, recording the supplied reason. Not
    /// permitted once the dinner has started — see Cookbook Recipe 9 §697.
    /// </summary>
    [HttpPost("{dinnerId:DinnerId}/cancel")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<DinnerResponse>> CancelDinner(
        HostId hostId,
        DinnerId dinnerId,
        [FromBody] CancelDinnerRequest request,
        CancellationToken cancellationToken) =>
        await _sender.Send(new CancelDinnerCommand(hostId, dinnerId, request.Reason ?? string.Empty), cancellationToken)
            .ToHttpResponseAsync(
                body: dinner => dinner.Adapt<DinnerResponse>(),
                configure: opts => opts.WithETag(dinner => EntityTagValue.Strong(dinner.ETag)))
            .AsActionResultAsync<DinnerResponse>();
}
