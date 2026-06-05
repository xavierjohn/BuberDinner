namespace BuberDinner.Application.Reservations.Commands;

using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Reservation.Entities;
using BuberDinner.Domain.User.ValueObjects;
using Mediator;

public sealed class CreateReservationCommand : ICommand<Result<Reservation>>
{
    public DinnerId DinnerId { get; }
    public UserId GuestUserId { get; }
    public int GuestCount { get; }

    public CreateReservationCommand(DinnerId dinnerId, UserId guestUserId, int guestCount)
    {
        DinnerId = dinnerId;
        GuestUserId = guestUserId;
        GuestCount = guestCount;
    }
}
