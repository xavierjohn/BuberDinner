namespace BuberDinner.Application.Reservations.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Reservation.Entities;
using Mediator;

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
        CreateReservationCommand request, CancellationToken cancellationToken) =>
        await LoadDinnerAsync(request.DinnerId, cancellationToken)
            .EnsureAsync(
                d => d.Status == DinnerStatus.Upcoming,
                d => Error.InvalidInput.ForRule("reservation.dinner-not-upcoming",
                    $"Cannot reserve against a dinner whose status is {d.Status.Value}."))
            .BindAsync(_ => Reservation.TryCreate(
                request.DinnerId, request.GuestUserId, request.GuestCount, _clock))
            .TapAsync(reservation => _reservationRepository.Add(reservation, cancellationToken));

    private async ValueTask<Result<Dinner>> LoadDinnerAsync(
        DinnerId dinnerId, CancellationToken cancellationToken) =>
        (await _dinnerRepository.FindById(dinnerId.Value.ToString(), cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<Dinner>(dinnerId)));
}
