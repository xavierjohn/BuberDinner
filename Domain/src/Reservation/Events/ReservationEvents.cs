namespace BuberDinner.Domain.Reservation.Events;

using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Reservation.ValueObject;
using BuberDinner.Domain.User.ValueObjects;

public sealed record ReservationCreated(
    ReservationId ReservationId,
    DinnerId DinnerId,
    UserId GuestUserId,
    int GuestCount,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ReservationCancelled(
    ReservationId ReservationId,
    DinnerId DinnerId,
    UserId GuestUserId,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;
