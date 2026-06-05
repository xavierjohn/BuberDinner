namespace BuberDinner.Application.Reservations.Queries;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Reservation.Entities;
using BuberDinner.Domain.Reservation.ValueObject;
using BuberDinner.Domain.User.ValueObjects;
using Mediator;

public sealed class GetReservationQuery : IRequest<Result<Reservation>>
{
    public ReservationId ReservationId { get; }
    public UserId CallerGuestUserId { get; }

    public GetReservationQuery(ReservationId reservationId, UserId callerGuestUserId)
    {
        ReservationId = reservationId;
        CallerGuestUserId = callerGuestUserId;
    }
}

public sealed class GetReservationQueryHandler : IRequestHandler<GetReservationQuery, Result<Reservation>>
{
    private readonly IReservationRepository _repo;

    public GetReservationQueryHandler(IReservationRepository repo)
    {
        _repo = repo;
    }

    public async ValueTask<Result<Reservation>> Handle(GetReservationQuery request, CancellationToken cancellationToken)
    {
        var reservation = await _repo.FindById(request.ReservationId.Value.ToString(), cancellationToken);
        if (reservation is null)
            return Result.Fail<Reservation>(new Error.NotFound(ResourceRef.For<Reservation>(request.ReservationId)));
        if (reservation.GuestUserId != request.CallerGuestUserId)
            return Result.Fail<Reservation>(new Error.NotFound(ResourceRef.For<Reservation>(request.ReservationId))
            {
                Detail = "Reservation does not belong to the calling guest.",
            });
        return Result.Ok(reservation);
    }
}
