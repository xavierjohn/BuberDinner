namespace BuberDinner.Application.Dinners.Commands;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Host.Entities;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu.ValueObject;
using Mediator;
using Trellis.Authorization;

/// <summary>
/// Schedules a new <see cref="Dinner"/> owned by the supplied <see cref="HostId"/>.
/// Implements <see cref="ICommand{TResponse}"/> (not <c>IRequest</c>) so the
/// <c>DomainEventDispatchBehavior</c> pipeline picks up the resulting aggregate's
/// <c>UncommittedEvents()</c> and fans <see cref="Domain.Dinner.Events.DinnerScheduled"/>
/// out to every registered handler.
/// </summary>
/// <remarks>
/// Authorization is resource-based via <see cref="IAuthorizeResource{TResource}"/> +
/// <see cref="IIdentifyResource{TResource, TId}"/>: the authenticated actor must equal
/// <see cref="Host.OwnerId"/>. The handler additionally verifies that the supplied
/// <see cref="MenuId"/> belongs to the same host (returns <see cref="Error.NotFound"/>
/// otherwise) — without that check a host could schedule against another host's menu.
/// </remarks>
public sealed class ScheduleDinnerCommand
    : ICommand<Result<Dinner>>, IAuthorizeResource<Host>, IIdentifyResource<Host, HostId>
{
    public HostId HostId { get; }
    public MenuId MenuId { get; }
    public Name Name { get; }
    public Description Description { get; }
    public DateTimeOffset StartDateTime { get; }
    public DateTimeOffset EndDateTime { get; }

    public static Result<ScheduleDinnerCommand> TryCreate(
        HostId hostId, MenuId menuId, Name name, Description description,
        DateTimeOffset startDateTime, DateTimeOffset endDateTime) =>
        Result.Ok(new ScheduleDinnerCommand(hostId, menuId, name, description, startDateTime, endDateTime));

    private ScheduleDinnerCommand(
        HostId hostId, MenuId menuId, Name name, Description description,
        DateTimeOffset startDateTime, DateTimeOffset endDateTime)
    {
        HostId = hostId;
        MenuId = menuId;
        Name = name;
        Description = description;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
    }

    public HostId GetResourceId() => HostId;

    public IResult Authorize(Actor actor, Host host) =>
        host.OwnerId.Value == actor.Id.Value
            ? Result.Ok()
            : Result.Fail(new Error.Forbidden("dinners.owner", ResourceRef.For<Host>(HostId)));
}
