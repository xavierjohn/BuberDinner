namespace BuberDinner.Domain.Dinner.Entities;

using System;
using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Dinner.Events;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu.ValueObject;
using FluentValidation;
using Stateless;
using Trellis.StateMachine;

/// <summary>
/// A dinner is a scheduled meal hosted by a <see cref="Host"/>, drawn from a particular
/// <see cref="Menu"/>. Its lifecycle is driven by a Stateless state machine (Cookbook
/// Recipe 9) wired through <see cref="LazyStateMachine{TState, TTrigger}"/>, with one
/// domain event raised per successful transition (Cookbook Recipe 17).
/// </summary>
public sealed class Dinner : Aggregate<DinnerId>
{
    public Name Name { get; }
    public Description Description { get; }
    public HostId HostId { get; }
    public MenuId MenuId { get; }
    public DateTimeOffset StartDateTime { get; }
    public DateTimeOffset EndDateTime { get; }
    public DinnerStatus Status { get; private set; }

    /// <summary>Wall-clock instant at which <see cref="Start"/> succeeded. Null until then.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Wall-clock instant at which <see cref="End"/> succeeded. Null until then.</summary>
    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>
    /// Wall-clock instant at which <see cref="Cancel"/> succeeded. Distinct from
    /// <see cref="EndedAt"/> so consumers can tell "the dinner ran" from "the dinner was
    /// called off". Null unless the dinner was cancelled.
    /// </summary>
    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>Reason supplied to <see cref="Cancel"/>. Null unless the dinner was cancelled.</summary>
    public string? CancellationReason { get; private set; }

    private readonly LazyStateMachine<DinnerStatus, DinnerTrigger> _machine;

    /// <summary>
    /// Schedules a new dinner. Returns a validated <see cref="Result{Dinner}"/> in the
    /// <see cref="DinnerStatus.Upcoming"/> state, with a <see cref="DinnerScheduled"/>
    /// domain event already queued for dispatch.
    /// </summary>
    public static Result<Dinner> TryCreate(
        Name name,
        Description description,
        HostId hostId,
        MenuId menuId,
        DateTimeOffset startDateTime,
        DateTimeOffset endDateTime,
        TimeProvider clock) =>
        s_inputValidator.ValidateToResult(new CreateInputs(
            name, description, hostId, menuId, startDateTime, endDateTime))
            .Map(inputs =>
            {
                var dinner = new Dinner(
                    DinnerId.NewUniqueV7(),
                    inputs.Name, inputs.Description, inputs.HostId, inputs.MenuId,
                    inputs.StartDateTime, inputs.EndDateTime);
                dinner.DomainEvents.Add(new DinnerScheduled(
                    dinner.Id, dinner.HostId, dinner.MenuId,
                    dinner.StartDateTime, dinner.EndDateTime, clock.GetUtcNow()));
                return dinner;
            });

    private sealed record CreateInputs(
        Name Name,
        Description Description,
        HostId HostId,
        MenuId MenuId,
        DateTimeOffset StartDateTime,
        DateTimeOffset EndDateTime);

    private Dinner(
        DinnerId id,
        Name name,
        Description description,
        HostId hostId,
        MenuId menuId,
        DateTimeOffset startDateTime,
        DateTimeOffset endDateTime)
        : base(id)
    {
        Name = name;
        Description = description;
        HostId = hostId;
        MenuId = menuId;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
        Status = DinnerStatus.Upcoming;

        _machine = new LazyStateMachine<DinnerStatus, DinnerTrigger>(
            stateAccessor: () => Status,
            stateMutator: s => Status = s,
            configure: ConfigureMachine);
    }

    /// <summary>
    /// Transitions the dinner from <see cref="DinnerStatus.Upcoming"/> to
    /// <see cref="DinnerStatus.InProgress"/>, sets <see cref="StartedAt"/>, and raises
    /// <see cref="DinnerStarted"/>. Returns <see cref="Error.InvalidInput"/> with reason
    /// code <c>state.machine.invalid.transition</c> (HTTP 422) when the current state
    /// does not permit the transition.
    /// </summary>
    public Result<Dinner> Start(TimeProvider clock) =>
        _machine.FireResult(DinnerTrigger.Start)
            .Map(_ =>
            {
                var occurredAt = clock.GetUtcNow();
                StartedAt = occurredAt;
                DomainEvents.Add(new DinnerStarted(Id, HostId, MenuId, occurredAt));
                return this;
            });

    /// <summary>
    /// Transitions the dinner from <see cref="DinnerStatus.InProgress"/> to
    /// <see cref="DinnerStatus.Ended"/>, sets <see cref="EndedAt"/>, and raises
    /// <see cref="DinnerEnded"/>.
    /// </summary>
    public Result<Dinner> End(TimeProvider clock) =>
        _machine.FireResult(DinnerTrigger.End)
            .Map(_ =>
            {
                var occurredAt = clock.GetUtcNow();
                EndedAt = occurredAt;
                DomainEvents.Add(new DinnerEnded(Id, HostId, MenuId, occurredAt));
                return this;
            });

    /// <summary>
    /// Transitions the dinner from <see cref="DinnerStatus.Upcoming"/> to
    /// <see cref="DinnerStatus.Cancelled"/>, records the supplied <paramref name="reason"/>,
    /// sets <see cref="CancelledAt"/>, and raises <see cref="DinnerCancelled"/>. Not
    /// permitted once the dinner has started.
    /// </summary>
    public Result<Dinner> Cancel(string reason, TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Fail<Dinner>(
                Error.InvalidInput.ForField(nameof(reason), "dinner.cancel.reason-required",
                    "Cancellation reason must not be blank."));

        return _machine.FireResult(DinnerTrigger.Cancel)
            .Map(_ =>
            {
                var occurredAt = clock.GetUtcNow();
                CancelledAt = occurredAt;
                CancellationReason = reason;
                DomainEvents.Add(new DinnerCancelled(Id, HostId, MenuId, reason, occurredAt));
                return this;
            });
    }

    private static void ConfigureMachine(StateMachine<DinnerStatus, DinnerTrigger> machine)
    {
        machine.Configure(DinnerStatus.Upcoming)
               .Permit(DinnerTrigger.Start, DinnerStatus.InProgress)
               .Permit(DinnerTrigger.Cancel, DinnerStatus.Cancelled);

        machine.Configure(DinnerStatus.InProgress)
               .Permit(DinnerTrigger.End, DinnerStatus.Ended);

        // Ended and Cancelled are terminal — no transitions configured.
    }

    static readonly InlineValidator<CreateInputs> s_inputValidator = new()
    {
        v => v.RuleFor(x => x.Name).NotEmpty(),
        v => v.RuleFor(x => x.Description).NotEmpty(),
        v => v.RuleFor(x => x.HostId).NotEmpty(),
        v => v.RuleFor(x => x.MenuId).NotEmpty(),
        v => v.RuleFor(x => x.StartDateTime).NotEqual(default(DateTimeOffset))
              .WithErrorCode("dinner.invalid.start-required")
              .WithMessage("StartDateTime is required."),
        v => v.RuleFor(x => x.EndDateTime).NotEqual(default(DateTimeOffset))
              .WithErrorCode("dinner.invalid.end-required")
              .WithMessage("EndDateTime is required."),
        v => v.RuleFor(x => x.EndDateTime)
              .Must((inputs, end) => end > inputs.StartDateTime)
              .WithErrorCode("dinner.invalid.schedule")
              .WithMessage("EndDateTime must be strictly after StartDateTime.")
              .When(x => x.StartDateTime != default && x.EndDateTime != default),
    };
}
