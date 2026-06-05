namespace BuberDinner.Application.Reservations.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Reservation.Entities;
using BuberDinner.Domain.Reservation.ValueObject;
using BuberDinner.Domain.User.ValueObjects;
using Mediator;

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
        CancelReservationCommand request, CancellationToken cancellationToken) =>
        await LoadReservationOwnedByAsync(request.ReservationId, request.CallerGuestUserId, cancellationToken)
            .BindAsync(reservation => reservation.Cancel(request.Reason, _clock))
            .TapAsync(reservation => _repo.Update(reservation, cancellationToken));

    private async ValueTask<Result<Reservation>> LoadReservationOwnedByAsync(
        ReservationId reservationId, UserId callerGuestUserId, CancellationToken cancellationToken) =>
        (await _repo.FindById(reservationId.Value.ToString(), cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<Reservation>(reservationId)))
            .Ensure(
                r => r.GuestUserId == callerGuestUserId,
                new Error.NotFound(ResourceRef.For<Reservation>(reservationId))
                {
                    Detail = "Reservation does not belong to the calling guest.",
                });
}
