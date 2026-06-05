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
    /// Lists dinners owned by the route host with cursor-based pagination (Cookbook Recipe 3).
    /// <list type="bullet">
    ///   <item><c>?limit=N</c> requests at most <c>N</c> items per page; server caps at <c>PageSize.Max</c>.
    ///         Omitted/zero/negative → <c>PageSize.Default</c>.</item>
    ///   <item><c>?cursor=&lt;opaque&gt;</c> resumes from the previous page's <c>next</c> token.
    ///         Malformed cursors → 422 with reason code <c>cursor.malformed</c>.</item>
    /// </list>
    /// Uses Trellis.Asp's <c>Result&lt;Page&lt;T&gt;&gt;.ToHttpResponseAsync</c> overload so the
    /// envelope, RFC 8288 <c>Link</c> header, and next/previous <c>PageLink</c> records are all
    /// emitted by the framework.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<Trellis.Asp.PagedResponse<DinnerResponse>>> ListDinners(
        HostId hostId,
        [FromQuery(Name = "cursor")] string? cursor,
        [FromQuery(Name = "limit")] int? limit,
        CancellationToken cancellationToken)
    {
        // Framework contract (trellis-api-asp.md:86 / :383): nextUrlBuilder must return an
        // ABSOLUTE URL. The cursor flows into PageLink.Href on the JSON envelope AND the
        // RFC 8288 `Link: <href>; rel="next"` header — both downstream consumers (clients
        // queueing the cursor for later, share-the-cursor flows, HATEOAS schedulers) expect
        // a fully-qualified URI they can hand to `new Uri(...)` without a base.
        var origin = $"{Request.Scheme}://{Request.Host}";
        var basePath = $"{origin}/hosts/{hostId.Value}/dinners";
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "2022-10-01";

        return await _sender.Send(
                new ListDinnersForHostQuery(
                    hostId,
                    cursor is { Length: > 0 } token ? new Cursor(token) : (Cursor?)null,
                    limit),
                cancellationToken)
            .ToHttpResponseAsync(
                nextUrlBuilder: (nextCursor, appliedLimit) =>
                    $"{basePath}?cursor={Uri.EscapeDataString(nextCursor.Token)}&limit={appliedLimit}&api-version={apiVersion}",
                body: dinner => dinner.Adapt<DinnerResponse>())
            .AsActionResultAsync<Trellis.Asp.PagedResponse<DinnerResponse>>();
    }

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

    /// <summary>
    /// Paginated list of every reservation against the route dinner — the host's view of
    /// who's coming. Gated by Host ownership (Cookbook Recipe 7) via
    /// <see cref="Trellis.Authorization.IAuthorizeResource{T}"/> on the underlying query.
    /// Defense-in-depth: handler additionally verifies the dinner belongs to the route host.
    /// </summary>
    [HttpGet("{dinnerId:DinnerId}/reservations")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<PagedResponse<BuberDinner.Api._2022_12_21.Models.Reservations.ReservationResponse>>> ListReservationsForDinner(
        HostId hostId,
        DinnerId dinnerId,
        [FromQuery(Name = "cursor")] string? cursor,
        [FromQuery(Name = "limit")] int? limit,
        CancellationToken cancellationToken)
    {
        var origin = $"{Request.Scheme}://{Request.Host}";
        var basePath = $"{origin}/hosts/{hostId.Value}/dinners/{dinnerId.Value}/reservations";
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "2022-10-01";

        return await _sender.Send(
                new BuberDinner.Application.Reservations.Queries.ListReservationsForDinnerQuery(
                    hostId, dinnerId,
                    cursor is { Length: > 0 } token ? new Cursor(token) : (Cursor?)null,
                    limit),
                cancellationToken)
            .ToHttpResponseAsync(
                nextUrlBuilder: (nextCursor, appliedLimit) =>
                    $"{basePath}?cursor={Uri.EscapeDataString(nextCursor.Token)}&limit={appliedLimit}&api-version={apiVersion}",
                body: reservation => reservation.Adapt<BuberDinner.Api._2022_12_21.Models.Reservations.ReservationResponse>())
            .AsActionResultAsync<PagedResponse<BuberDinner.Api._2022_12_21.Models.Reservations.ReservationResponse>>();
    }
}
