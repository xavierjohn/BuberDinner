namespace BuberDinner.Domain.Tests;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Dinner.Events;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu.ValueObject;
using Microsoft.Extensions.Time.Testing;

/// <summary>
/// Domain tests for the <see cref="Dinner"/> aggregate state machine and the four
/// transition-driven domain events. Uses <see cref="FakeTimeProvider"/> so every event's
/// <c>OccurredAt</c> is deterministic and can be asserted by value.
/// </summary>
public class DinnerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 18, 30, 0, TimeSpan.Zero);
    private static readonly HostId Host = HostId.NewUniqueV7();
    private static readonly MenuId Menu = MenuId.NewUniqueV7();

    private static FakeTimeProvider Clock(DateTimeOffset? when = null) => new(when ?? Now);

    private static Dinner NewUpcoming(TimeProvider clock)
    {
        var dinner = Dinner.TryCreate(
            Name.TryCreate("Brunch").GetValueOrThrow(),
            Description.TryCreate("Sunday brunch").GetValueOrThrow(),
            Host, Menu,
            startDateTime: Now.AddHours(2),
            endDateTime: Now.AddHours(4),
            clock).GetValueOrThrow();
        // TryCreate stamps DinnerScheduled. Tests that don't care about it can drain the buffer.
        return dinner;
    }

    [Fact]
    public void TryCreate_seeds_Upcoming_and_raises_DinnerScheduled()
    {
        var clock = Clock();
        var dinner = NewUpcoming(clock);

        dinner.Status.Should().Be(DinnerStatus.Upcoming);
        dinner.StartedAt.Should().BeNull();
        dinner.EndedAt.Should().BeNull();
        dinner.CancelledAt.Should().BeNull();

        var ev = dinner.UncommittedEvents().Should().ContainSingle().Which.Should().BeOfType<DinnerScheduled>().Subject;
        ev.OccurredAt.Should().Be(Now);
        ev.HostId.Should().Be(Host);
        ev.MenuId.Should().Be(Menu);
        ev.StartDateTime.Should().Be(Now.AddHours(2));
        ev.EndDateTime.Should().Be(Now.AddHours(4));
    }

    [Fact]
    public void TryCreate_rejects_end_before_start()
    {
        var clock = Clock();
        var bad = Dinner.TryCreate(
            Name.TryCreate("Bad").GetValueOrThrow(),
            Description.TryCreate("Bad").GetValueOrThrow(),
            Host, Menu,
            startDateTime: Now.AddHours(4),
            endDateTime: Now.AddHours(2),
            clock);
        bad.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Start_succeeds_from_Upcoming_and_raises_DinnerStarted()
    {
        var clock = Clock();
        var dinner = NewUpcoming(clock);
        dinner.AcceptChanges(); // drain DinnerScheduled

        clock.Advance(TimeSpan.FromHours(2));
        var result = dinner.Start(clock);

        result.IsSuccess.Should().BeTrue();
        dinner.Status.Should().Be(DinnerStatus.InProgress);
        dinner.StartedAt.Should().Be(Now.AddHours(2));

        var ev = dinner.UncommittedEvents().Should().ContainSingle().Which.Should().BeOfType<DinnerStarted>().Subject;
        ev.OccurredAt.Should().Be(Now.AddHours(2));
        ev.HostId.Should().Be(Host);
        ev.MenuId.Should().Be(Menu);
    }

    [Fact]
    public void End_from_Upcoming_is_rejected_with_state_machine_reason_code()
    {
        var clock = Clock();
        var dinner = NewUpcoming(clock);

        var result = dinner.End(clock);

        result.IsFailure.Should().BeTrue();
        var error = result.Match(_ => null!, e => e).Should().BeOfType<Error.InvalidInput>().Subject;
        error.Rules.Items.Should().ContainSingle()
            .Which.ReasonCode.Should().Be("state.machine.invalid.transition");
        dinner.Status.Should().Be(DinnerStatus.Upcoming, "rejected transitions must not mutate state");
        dinner.EndedAt.Should().BeNull();
    }

    [Fact]
    public void End_succeeds_from_InProgress_and_raises_DinnerEnded()
    {
        var clock = Clock();
        var dinner = NewUpcoming(clock);
        dinner.Start(clock).GetValueOrThrow();
        dinner.AcceptChanges();

        clock.Advance(TimeSpan.FromHours(2));
        var result = dinner.End(clock);

        result.IsSuccess.Should().BeTrue();
        dinner.Status.Should().Be(DinnerStatus.Ended);
        dinner.EndedAt.Should().Be(Now.AddHours(2));
        var ev = dinner.UncommittedEvents().Should().ContainSingle().Which.Should().BeOfType<DinnerEnded>().Subject;
        ev.OccurredAt.Should().Be(Now.AddHours(2));
    }

    [Fact]
    public void Cancel_succeeds_from_Upcoming_and_raises_DinnerCancelled_with_reason()
    {
        var clock = Clock();
        var dinner = NewUpcoming(clock);
        dinner.AcceptChanges();

        var result = dinner.Cancel("host illness", clock);

        result.IsSuccess.Should().BeTrue();
        dinner.Status.Should().Be(DinnerStatus.Cancelled);
        dinner.CancelledAt.Should().Be(Now);
        dinner.CancellationReason.Should().Be("host illness");
        dinner.EndedAt.Should().BeNull("Cancelled and Ended are semantically distinct");
        var ev = dinner.UncommittedEvents().Should().ContainSingle().Which.Should().BeOfType<DinnerCancelled>().Subject;
        ev.Reason.Should().Be("host illness");
        ev.OccurredAt.Should().Be(Now);
    }

    [Fact]
    public void Cancel_from_InProgress_is_rejected_with_state_machine_reason_code()
    {
        var clock = Clock();
        var dinner = NewUpcoming(clock);
        dinner.Start(clock).GetValueOrThrow();
        dinner.AcceptChanges();

        var result = dinner.Cancel("too late", clock);

        result.IsFailure.Should().BeTrue();
        var error = result.Match(_ => null!, e => e).Should().BeOfType<Error.InvalidInput>().Subject;
        error.Rules.Items.Should().ContainSingle()
            .Which.ReasonCode.Should().Be("state.machine.invalid.transition");
        dinner.Status.Should().Be(DinnerStatus.InProgress);
        dinner.CancellationReason.Should().BeNull();
    }

    [Fact]
    public void Cancel_rejects_blank_reason_before_consulting_state_machine()
    {
        var clock = Clock();
        var dinner = NewUpcoming(clock);
        dinner.AcceptChanges();

        var result = dinner.Cancel("   ", clock);

        result.IsFailure.Should().BeTrue();
        var error = result.Match(_ => null!, e => e).Should().BeOfType<Error.InvalidInput>().Subject;
        error.Fields.Items.Should().ContainSingle()
            .Which.ReasonCode.Should().Be("dinner.cancel.reason-required");
        dinner.Status.Should().Be(DinnerStatus.Upcoming);
    }

    [Fact]
    public void Start_from_Ended_is_rejected()
    {
        var clock = Clock();
        var dinner = NewUpcoming(clock);
        dinner.Start(clock).GetValueOrThrow();
        dinner.End(clock).GetValueOrThrow();
        dinner.AcceptChanges();

        var result = dinner.Start(clock);

        result.IsFailure.Should().BeTrue();
        dinner.Status.Should().Be(DinnerStatus.Ended);
    }

    [Fact]
    public void Start_from_Cancelled_is_rejected()
    {
        var clock = Clock();
        var dinner = NewUpcoming(clock);
        dinner.Cancel("no longer needed", clock).GetValueOrThrow();
        dinner.AcceptChanges();

        var result = dinner.Start(clock);

        result.IsFailure.Should().BeTrue();
        dinner.Status.Should().Be(DinnerStatus.Cancelled);
    }
}

