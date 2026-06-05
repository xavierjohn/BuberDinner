namespace BuberDinner._2022_12_21.Controllers;

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using BuberDinner.Api._2022_12_21.Models.Reservations;
using BuberDinner.Application.Reservations.Commands;
using BuberDinner.Application.Reservations.Queries;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Reservation.Entities;
using BuberDinner.Domain.Reservation.ValueObject;
using BuberDinner.Domain.User.ValueObjects;
using Mapster;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Trellis;
using Trellis.Asp;
using Trellis.Asp.Idempotency;

/// <summary>
/// Reservation endpoints. The create endpoint opts into the IETF Idempotency-Key middleware
/// (Cookbook Recipe 29) via <see cref="IdempotentAttribute"/> — a retry with the same
/// <c>Idempotency-Key</c> header + same body returns the cached 201 byte-for-byte instead
/// of creating a second reservation. Same key + different body → 422 fingerprint mismatch.
/// </summary>
[ApiVersion("2022-10-01")]
[Route("reservations")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class ReservationsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initialises a new instance of <see cref="ReservationsController"/>.</summary>
    public ReservationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Reserve a seat at a dinner. Idempotent via the <c>Idempotency-Key</c> header per
    /// IETF draft-ietf-httpapi-idempotency-key-header. Recipe 22 fail-loud applies: the
    /// handler returns 404 if the supplied DinnerId doesn't exist, and 422 if the dinner
    /// is no longer in the <c>Upcoming</c> state.
    /// </summary>
    [HttpPost]
    [Idempotent]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<ReservationResponse>> CreateReservation(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var guestIdResult = ResolveCallerGuestId();
        if (guestIdResult.IsFailure)
            return Unauthorized();
        var guestId = guestIdResult.GetValueOrThrow("guestId");

        var dinnerIdResult = DinnerId.TryCreate(request.DinnerId);
        if (dinnerIdResult.IsFailure)
            return UnprocessableEntity(new { errors = new { dinnerId = new[] { "Invalid DinnerId." } } });
        var dinnerId = dinnerIdResult.GetValueOrThrow("dinnerId");

        var command = new CreateReservationCommand(dinnerId, guestId, request.GuestCount);
        return await _sender.Send(command, cancellationToken)
            .ToHttpResponseAsync(
                body: reservation => reservation.Adapt<ReservationResponse>(),
                configure: opts => opts
                    .Created(reservation => $"/reservations/{reservation.Id.Value}")
                    .WithETag(reservation => EntityTagValue.Strong(reservation.ETag)))
            .AsActionResultAsync<ReservationResponse>();
    }

    /// <summary>Get a single reservation. Visible only to the owning guest.</summary>
    [HttpGet("{reservationId:ReservationId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<ReservationResponse>> GetReservation(
        ReservationId reservationId, CancellationToken cancellationToken)
    {
        var guestIdResult = ResolveCallerGuestId();
        if (guestIdResult.IsFailure)
            return Unauthorized();
        var guestId = guestIdResult.GetValueOrThrow("guestId");

        return await _sender.Send(new GetReservationQuery(reservationId, guestId), cancellationToken)
            .ToHttpResponseAsync(
                body: reservation => reservation.Adapt<ReservationResponse>(),
                configure: opts => opts
                    .WithETag(reservation => EntityTagValue.Strong(reservation.ETag))
                    .EvaluatePreconditions())
            .AsActionResultAsync<ReservationResponse>();
    }

    /// <summary>Cancel an active reservation. Only the owning guest can cancel.</summary>
    [HttpPost("{reservationId:ReservationId}/cancel")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<ReservationResponse>> CancelReservation(
        ReservationId reservationId,
        [FromBody] CancelReservationRequest request,
        CancellationToken cancellationToken)
    {
        var guestIdResult = ResolveCallerGuestId();
        if (guestIdResult.IsFailure)
            return Unauthorized();
        var guestId = guestIdResult.GetValueOrThrow("guestId");

        var command = new CancelReservationCommand(reservationId, guestId, request.Reason ?? string.Empty);
        return await _sender.Send(command, cancellationToken)
            .ToHttpResponseAsync(
                body: reservation => reservation.Adapt<ReservationResponse>(),
                configure: opts => opts.WithETag(reservation => EntityTagValue.Strong(reservation.ETag)))
            .AsActionResultAsync<ReservationResponse>();
    }

    /// <summary>
    /// Paginated list of the calling guest's own reservations. Cursor + limit per Cookbook
    /// Recipe 3 — same envelope shape as PR 3's other list endpoints.
    /// </summary>
    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<PagedResponse<ReservationResponse>>> ListMyReservations(
        [FromQuery(Name = "cursor")] string? cursor,
        [FromQuery(Name = "limit")] int? limit,
        CancellationToken cancellationToken)
    {
        var guestIdResult = ResolveCallerGuestId();
        if (guestIdResult.IsFailure)
            return Unauthorized();
        var guestId = guestIdResult.GetValueOrThrow("guestId");

        var origin = $"{Request.Scheme}://{Request.Host}";
        var basePath = $"{origin}/reservations/mine";
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "2022-10-01";

        return await _sender.Send(
                new ListMyReservationsQuery(
                    guestId,
                    cursor is { Length: > 0 } token ? new Cursor(token) : (Cursor?)null,
                    limit),
                cancellationToken)
            .ToHttpResponseAsync(
                nextUrlBuilder: (nextCursor, appliedLimit) =>
                    $"{basePath}?cursor={Uri.EscapeDataString(nextCursor.Token)}&limit={appliedLimit}&api-version={apiVersion}",
                body: reservation => reservation.Adapt<ReservationResponse>())
            .AsActionResultAsync<PagedResponse<ReservationResponse>>();
    }

    private Result<UserId> ResolveCallerGuestId()
    {
        var raw = HttpContext.User.FindFirst("sub")?.Value
                  ?? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(raw))
            return Result.Fail<UserId>(new Error.AuthenticationRequired());
        return UserId.TryCreate(raw);
    }
}
