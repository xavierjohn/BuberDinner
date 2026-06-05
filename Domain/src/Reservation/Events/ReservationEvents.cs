namespace BuberDinner.Domain.Reservation.Events;

using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Reservation.ValueObject;
using BuberDinner.Domain.User.ValueObjects;

/// <summary>Raised when a guest creates a new reservation against an upcoming dinner.</summary>
public sealed record ReservationCreated(
    ReservationId ReservationId,
    DinnerId DinnerId,
    UserId GuestUserId,
    int GuestCount,
    DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised when a guest cancels a previously-active reservation.</summary>
public sealed record ReservationCancelled(
    ReservationId ReservationId,
    DinnerId DinnerId,
    UserId GuestUserId,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;
