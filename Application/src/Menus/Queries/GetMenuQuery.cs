namespace BuberDinner.Application.Menus.Queries;

using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu;
using BuberDinner.Domain.Menu.ValueObject;
using Mediator;

public sealed class GetMenuQuery : IRequest<Result<Menu>>
{
    public HostId HostId { get; }
    public MenuId MenuId { get; }

    public GetMenuQuery(HostId hostId, MenuId menuId)
    {
        HostId = hostId;
        MenuId = menuId;
    }
}
