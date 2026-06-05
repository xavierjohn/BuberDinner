namespace BuberDinner.Application.Reservations.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Reservation.Entities;
using BuberDinner.Domain.Reservation.ValueObject;
using BuberDinner.Domain.User.ValueObjects;
using Mediator;

/// <summary>
/// Cancels an active reservation. Authorisation is handled inside the handler (rather than
/// via <see cref="Trellis.Authorization.IAuthorizeResource{T}"/>) because the resource the
/// caller must own is the Reservation itself — keyed by <c>GuestUserId</c> — and the existing
/// resource-auth machinery wires through Host-id parents. A future PR can switch this to a
/// shared <c>SharedResourceLoaderById&lt;Reservation, ReservationId&gt;</c> + Actor.Id match.
/// </summary>
public sealed class CancelReservationCommand : ICommand<Result<Reservation>>
{
    public ReservationId ReservationId { get; }
    public UserId CallerGuestUserId { get; }
    public string Reason { get; }

    public CancelReservationCommand(ReservationId reservationId, UserId callerGuestUserId, string reason)
    {
        ReservationId = reservationId;
        CallerGuestUserId = callerGuestUserId;
        Reason = reason;
    }
}

public sealed class CancelReservationCommandHandler
    : ICommandHandler<CancelReservationCommand, Result<Reservation>>
{
    private readonly IReservationRepository _repo;
    private readonly TimeProvider _clock;

    public CancelReservationCommandHandler(IReservationRepository repo, TimeProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public async ValueTask<Result<Reservation>> Handle(
        CancelReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await _repo.FindById(request.ReservationId.Value.ToString(), cancellationToken);
        if (reservation is null)
            return Result.Fail<Reservation>(new Error.NotFound(ResourceRef.For<Reservation>(request.ReservationId)));

        // Only the guest who created the reservation can cancel it. Mirrors the
        // dinners.owner / menus.owner pattern from PR 1/2; uses NotFound (not Forbidden)
        // to avoid leaking existence of reservations the caller doesn't own.
        if (reservation.GuestUserId != request.CallerGuestUserId)
            return Result.Fail<Reservation>(new Error.NotFound(ResourceRef.For<Reservation>(request.ReservationId))
            {
                Detail = "Reservation does not belong to the calling guest.",
            });

        var cancelResult = reservation.Cancel(request.Reason, _clock);
        if (cancelResult.IsFailure)
            return cancelResult;

        await _repo.Update(reservation, cancellationToken);
        return cancelResult;
    }
}
