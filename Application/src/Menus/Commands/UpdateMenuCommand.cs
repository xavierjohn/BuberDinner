namespace BuberDinner.Application.Menus.Commands;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Host.Entities;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu;
using BuberDinner.Domain.Menu.ValueObject;
using Mediator;
using Trellis.Authorization;

/// <summary>
/// Updates an existing menu's name and description. Authorization is gated by the owning Host:
/// the authenticated actor must equal <see cref="Host.OwnerId"/>. The framework wires this via
/// <see cref="IAuthorizeResource{TResource}"/> + <see cref="IIdentifyResource{TResource, TId}"/>
/// and the shared <c>SharedResourceLoaderById&lt;Host, HostId&gt;</c> implementation in
/// <c>Application/Hosts/Authorization/HostResourceLoader.cs</c>.
///
/// Optimistic-concurrency control: the controller parses <c>If-Match</c> via
/// <c>ETagHelper.ParseIfMatch(...)</c> and threads the resulting <see cref="EntityTagValue"/>
/// array into <see cref="IfMatch"/>. The handler then runs <c>RequireETagAsync(IfMatch)</c>
/// at the read-modify-write boundary per Cookbook Recipe 23 / RFC 9110 §13.1.1:
/// missing → 428 Precondition Required; stale → 412 Precondition Failed.
/// </summary>
public sealed class UpdateMenuCommand
    : IRequest<Result<Menu>>, IAuthorizeResource<Host>, IIdentifyResource<Host, HostId>
{
    public HostId HostId { get; }
    public MenuId MenuId { get; }
    public Name Name { get; }
    public Description Description { get; }
    public EntityTagValue[]? IfMatch { get; }

    public static Result<UpdateMenuCommand> TryCreate(
        HostId hostId, MenuId menuId, Name name, Description description, EntityTagValue[]? ifMatch) =>
        Result.Ok(new UpdateMenuCommand(hostId, menuId, name, description, ifMatch));

    private UpdateMenuCommand(HostId hostId, MenuId menuId, Name name, Description description, EntityTagValue[]? ifMatch)
    {
        HostId = hostId;
        MenuId = menuId;
        Name = name;
        Description = description;
        IfMatch = ifMatch;
    }

    public HostId GetResourceId() => HostId;

    public IResult Authorize(Actor actor, Host host) =>
        host.OwnerId.Value == actor.Id.Value
            ? Result.Ok()
            : Result.Fail(new Error.Forbidden("menus.owner", ResourceRef.For<Host>(HostId)));
}
