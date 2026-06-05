namespace BuberDinner.Application.Reservations.Commands;

using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Reservation.Entities;
using BuberDinner.Domain.User.ValueObjects;
using Mediator;

/// <summary>
/// Creates a new reservation for the authenticated guest against the supplied dinner.
/// </summary>
/// <remarks>
/// Implemented as <see cref="ICommand{TResponse}"/> (not <c>IRequest</c>) so the
/// <c>DomainEventDispatchBehavior</c> pipeline picks up <c>ReservationCreated</c> and fans
/// it out to every registered handler — same rationale as the Dinner commands in PR 2.
/// Wrapped at the HTTP boundary by the IETF Idempotency-Key middleware
/// (<c>[Idempotent]</c> on the controller action, per Cookbook Recipe 29) so a network
/// retry with the same key + body returns the cached 201 instead of double-booking.
/// </remarks>
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
