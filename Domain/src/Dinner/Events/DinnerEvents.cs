namespace BuberDinner.Domain.Dinner.Events;

using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu.ValueObject;

/// <summary>
/// Raised when a host schedules a new dinner. Per Cookbook Recipe 17, <see cref="OccurredAt"/>
/// is the only timestamp; the event-type name carries the "scheduled" semantic.
/// </summary>
public sealed record DinnerScheduled(
    DinnerId DinnerId,
    HostId HostId,
    MenuId MenuId,
    DateTimeOffset StartDateTime,
    DateTimeOffset EndDateTime,
    DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised when a scheduled dinner transitions from <c>Upcoming</c> to <c>InProgress</c>.</summary>
public sealed record DinnerStarted(
    DinnerId DinnerId,
    HostId HostId,
    MenuId MenuId,
    DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised when a running dinner transitions from <c>InProgress</c> to <c>Ended</c>.</summary>
public sealed record DinnerEnded(
    DinnerId DinnerId,
    HostId HostId,
    MenuId MenuId,
    DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>
/// Raised when a scheduled dinner is called off before it begins. Carries the cancellation
/// reason so notification consumers can include it.
/// </summary>
public sealed record DinnerCancelled(
    DinnerId DinnerId,
    HostId HostId,
    MenuId MenuId,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;
