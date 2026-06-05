namespace BuberDinner.Domain.Tests;

using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Reservation.Entities;
using BuberDinner.Domain.Reservation.Events;
using BuberDinner.Domain.Reservation.ValueObject;
using BuberDinner.Domain.User.ValueObjects;
using Microsoft.Extensions.Time.Testing;

public class ReservationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 18, 30, 0, TimeSpan.Zero);
    private static readonly DinnerId Dinner = DinnerId.NewUniqueV7();
    private static readonly UserId Guest = UserId.TryCreate("guest_user_42").GetValueOrThrow();

    private static FakeTimeProvider Clock(DateTimeOffset? when = null) => new(when ?? Now);

    private static Reservation NewReserved(TimeProvider clock) =>
        Reservation.TryCreate(Dinner, Guest, guestCount: 2, clock).GetValueOrThrow();

    [Fact]
    public void TryCreate_succeeds_with_Reserved_status_and_raises_ReservationCreated()
    {
        var clock = Clock();
        var r = NewReserved(clock);

        r.Status.Should().Be(ReservationStatus.Reserved);
        r.GuestCount.Should().Be(2);
        r.GuestUserId.Should().Be(Guest);
        r.DinnerId.Should().Be(Dinner);
        r.ReservedAt.Should().Be(Now);
        r.CancelledAt.Should().BeNull();
        r.CancellationReason.Should().BeNull();

        var ev = r.UncommittedEvents().Should().ContainSingle().Which.Should().BeOfType<ReservationCreated>().Subject;
        ev.OccurredAt.Should().Be(Now);
        ev.GuestCount.Should().Be(2);
    }

    [Fact]
    public void TryCreate_rejects_zero_guest_count()
    {
        var bad = Reservation.TryCreate(Dinner, Guest, guestCount: 0, Clock());
        bad.IsFailure.Should().BeTrue();
        var error = bad.Match(_ => null!, e => e).Should().BeOfType<Error.InvalidInput>().Subject;
        error.Fields.Items.Should().ContainSingle()
            .Which.ReasonCode.Should().Be("reservation.invalid.guest-count");
    }

    [Fact]
    public void TryCreate_rejects_negative_guest_count()
    {
        var bad = Reservation.TryCreate(Dinner, Guest, guestCount: -1, Clock());
        bad.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Cancel_succeeds_from_Reserved_and_raises_ReservationCancelled()
    {
        var clock = Clock();
        var r = NewReserved(clock);
        r.AcceptChanges();

        clock.Advance(TimeSpan.FromHours(1));
        var result = r.Cancel("Schedule conflict", clock);

        result.IsSuccess.Should().BeTrue();
        r.Status.Should().Be(ReservationStatus.Cancelled);
        r.CancelledAt.Should().Be(Now.AddHours(1));
        r.CancellationReason.Should().Be("Schedule conflict");
        var ev = r.UncommittedEvents().Should().ContainSingle().Which.Should().BeOfType<ReservationCancelled>().Subject;
        ev.Reason.Should().Be("Schedule conflict");
        ev.OccurredAt.Should().Be(Now.AddHours(1));
    }

    [Fact]
    public void Cancel_when_already_Cancelled_is_rejected_with_state_machine_reason_code()
    {
        var clock = Clock();
        var r = NewReserved(clock);
        r.Cancel("First", clock).GetValueOrThrow();
        r.AcceptChanges();

        var result = r.Cancel("Second", clock);

        result.IsFailure.Should().BeTrue();
        var error = result.Match(_ => null!, e => e).Should().BeOfType<Error.InvalidInput>().Subject;
        error.Rules.Items.Should().ContainSingle()
            .Which.ReasonCode.Should().Be("state.machine.invalid.transition");
        r.CancellationReason.Should().Be("First", "rejected transitions must not overwrite prior state");
    }

    [Fact]
    public void Cancel_rejects_blank_reason_before_consulting_state_machine()
    {
        var clock = Clock();
        var r = NewReserved(clock);
        r.AcceptChanges();

        var result = r.Cancel("   ", clock);

        result.IsFailure.Should().BeTrue();
        var error = result.Match(_ => null!, e => e).Should().BeOfType<Error.InvalidInput>().Subject;
        error.Fields.Items.Should().ContainSingle()
            .Which.ReasonCode.Should().Be("reservation.cancel.reason-required");
        r.Status.Should().Be(ReservationStatus.Reserved);
    }
}
