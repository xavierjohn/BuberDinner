namespace BuberDinner.Application.Reservations.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Reservation.Entities;
using Mediator;

/// <summary>
/// Handles <see cref="CreateReservationCommand"/>: loads the parent Dinner (fail-loud if
/// missing per Cookbook Recipe 22), verifies it is still in <c>Upcoming</c>, builds the
/// Reservation aggregate, and persists. The dispatch pipeline then publishes
/// <c>ReservationCreated</c> after the handler returns successfully.
/// </summary>
public sealed class CreateReservationCommandHandler
    : ICommandHandler<CreateReservationCommand, Result<Reservation>>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IRepository<Dinner> _dinnerRepository;
    private readonly TimeProvider _clock;

    public CreateReservationCommandHandler(
        IReservationRepository reservationRepository,
        IRepository<Dinner> dinnerRepository,
        TimeProvider clock)
    {
        _reservationRepository = reservationRepository;
        _dinnerRepository = dinnerRepository;
        _clock = clock;
    }

    public async ValueTask<Result<Reservation>> Handle(
        CreateReservationCommand request, CancellationToken cancellationToken)
    {
        // Recipe 22 — fail-loud on missing related aggregate. Without this the create
        // command would silently succeed against a non-existent dinner, leaving an orphan
        // reservation row pointing at nothing. NotFound (not Forbidden) keeps existence
        // private from the caller.
        var dinner = await _dinnerRepository.FindById(request.DinnerId.Value.ToString(), cancellationToken);
        if (dinner is null)
            return Result.Fail<Reservation>(new Error.NotFound(ResourceRef.For<Dinner>(request.DinnerId)));

        // Capacity / state precondition: a reservation only makes sense on a dinner that's
        // still in the future. Started / ended / cancelled dinners are 422, not 404 — the
        // dinner exists, it just doesn't accept reservations any more.
        if (dinner.Status != DinnerStatus.Upcoming)
            return Result.Fail<Reservation>(
                Error.InvalidInput.ForRule("reservation.dinner-not-upcoming",
                    $"Cannot reserve against a dinner whose status is {dinner.Status.Value}."));

        var reservationResult = Reservation.TryCreate(
            request.DinnerId, request.GuestUserId, request.GuestCount, _clock);
        if (reservationResult.IsFailure)
            return reservationResult;
        var reservation = reservationResult.GetValueOrThrow("reservation");

        await _reservationRepository.Add(reservation, cancellationToken);
        return Result.Ok(reservation);
    }
}
