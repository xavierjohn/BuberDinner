namespace BuberDinner.Domain.Reservation.Entities;

using System;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Reservation.Events;
using BuberDinner.Domain.Reservation.ValueObject;
using BuberDinner.Domain.User.ValueObjects;
using FluentValidation;
using Stateless;
using Trellis.StateMachine;

public sealed class Reservation : Aggregate<ReservationId>
{
    public DinnerId DinnerId { get; }
    public UserId GuestUserId { get; }
    public int GuestCount { get; }
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset ReservedAt { get; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    private readonly LazyStateMachine<ReservationStatus, ReservationTrigger> _machine;

    public static Result<Reservation> TryCreate(
        DinnerId dinnerId,
        UserId guestUserId,
        int guestCount,
        TimeProvider clock)
    {
        if (guestCount <= 0)
            return Result.Fail<Reservation>(
                Error.InvalidInput.ForField(nameof(GuestCount), "reservation.invalid.guest-count",
                    "GuestCount must be positive."));

        var reservation = new Reservation(
            ReservationId.NewUniqueV7(), dinnerId, guestUserId, guestCount, clock.GetUtcNow());

        var validation = s_validator.ValidateToResult(reservation);
        if (validation.IsFailure)
            return validation;

        reservation.DomainEvents.Add(new ReservationCreated(
            reservation.Id, reservation.DinnerId, reservation.GuestUserId,
            reservation.GuestCount, reservation.ReservedAt));
        return Result.Ok(reservation);
    }

    private Reservation(
        ReservationId id,
        DinnerId dinnerId,
        UserId guestUserId,
        int guestCount,
        DateTimeOffset reservedAt)
        : base(id)
    {
        DinnerId = dinnerId;
        GuestUserId = guestUserId;
        GuestCount = guestCount;
        Status = ReservationStatus.Reserved;
        ReservedAt = reservedAt;

        _machine = new LazyStateMachine<ReservationStatus, ReservationTrigger>(
            stateAccessor: () => Status,
            stateMutator: s => Status = s,
            configure: ConfigureMachine);
    }

    public Result<Reservation> Cancel(string reason, TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Fail<Reservation>(
                Error.InvalidInput.ForField(nameof(reason), "reservation.cancel.reason-required",
                    "Cancellation reason must not be blank."));

        return _machine.FireResult(ReservationTrigger.Cancel)
            .Map(_ =>
            {
                var occurredAt = clock.GetUtcNow();
                CancelledAt = occurredAt;
                CancellationReason = reason;
                DomainEvents.Add(new ReservationCancelled(
                    Id, DinnerId, GuestUserId, reason, occurredAt));
                return this;
            });
    }

    private static void ConfigureMachine(StateMachine<ReservationStatus, ReservationTrigger> machine) =>
        machine.Configure(ReservationStatus.Reserved)
               .Permit(ReservationTrigger.Cancel, ReservationStatus.Cancelled);

    static readonly InlineValidator<Reservation> s_validator = new()
    {
        v => v.RuleFor(x => x.Id).NotEmpty(),
        v => v.RuleFor(x => x.DinnerId).NotEmpty(),
        v => v.RuleFor(x => x.GuestUserId).NotEmpty(),
        v => v.RuleFor(x => x.GuestCount).GreaterThan(0),
        v => v.RuleFor(x => x.Status).NotEmpty(),
    };
}
