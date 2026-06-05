namespace BuberDinner.Application.Dinners.Commands;

using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Host.Entities;
using BuberDinner.Domain.Host.ValueObject;
using Mediator;
using Trellis.Authorization;

/// <summary>
/// Transitions a <see cref="Dinner"/> from <c>Upcoming</c> to <c>InProgress</c>.
/// Body-less per Cookbook Recipe 23 (state-machine guard substitutes for <c>If-Match</c>).
/// </summary>
public sealed class StartDinnerCommand
    : ICommand<Result<Dinner>>, IAuthorizeResource<Host>, IIdentifyResource<Host, HostId>
{
    public HostId HostId { get; }
    public DinnerId DinnerId { get; }

    public StartDinnerCommand(HostId hostId, DinnerId dinnerId)
    {
        HostId = hostId;
        DinnerId = dinnerId;
    }

    public HostId GetResourceId() => HostId;

    public IResult Authorize(Actor actor, Host host) =>
        host.OwnerId.Value == actor.Id.Value
            ? Result.Ok()
            : Result.Fail(new Error.Forbidden("dinners.owner", ResourceRef.For<Host>(HostId)));
}

/// <summary>
/// Transitions a <see cref="Dinner"/> from <c>InProgress</c> to <c>Ended</c>.
/// </summary>
public sealed class EndDinnerCommand
    : ICommand<Result<Dinner>>, IAuthorizeResource<Host>, IIdentifyResource<Host, HostId>
{
    public HostId HostId { get; }
    public DinnerId DinnerId { get; }

    public EndDinnerCommand(HostId hostId, DinnerId dinnerId)
    {
        HostId = hostId;
        DinnerId = dinnerId;
    }

    public HostId GetResourceId() => HostId;

    public IResult Authorize(Actor actor, Host host) =>
        host.OwnerId.Value == actor.Id.Value
            ? Result.Ok()
            : Result.Fail(new Error.Forbidden("dinners.owner", ResourceRef.For<Host>(HostId)));
}

/// <summary>
/// Transitions a <see cref="Dinner"/> from <c>Upcoming</c> to <c>Cancelled</c>, recording
/// the supplied reason. Not permitted once the dinner has started.
/// </summary>
public sealed class CancelDinnerCommand
    : ICommand<Result<Dinner>>, IAuthorizeResource<Host>, IIdentifyResource<Host, HostId>
{
    public HostId HostId { get; }
    public DinnerId DinnerId { get; }
    public string Reason { get; }

    public CancelDinnerCommand(HostId hostId, DinnerId dinnerId, string reason)
    {
        HostId = hostId;
        DinnerId = dinnerId;
        Reason = reason;
    }

    public HostId GetResourceId() => HostId;

    public IResult Authorize(Actor actor, Host host) =>
        host.OwnerId.Value == actor.Id.Value
            ? Result.Ok()
            : Result.Fail(new Error.Forbidden("dinners.owner", ResourceRef.For<Host>(HostId)));
}
