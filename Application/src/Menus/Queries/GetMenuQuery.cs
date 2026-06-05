namespace BuberDinner.Application.Menus.Queries;

using BuberDinner.Domain.Menu;
using BuberDinner.Domain.Menu.ValueObject;
using Mediator;

public sealed class GetMenuQuery : IRequest<Result<Menu>>
{
    public MenuId MenuId { get; }

    public GetMenuQuery(MenuId menuId)
    {
        MenuId = menuId;
    }
}
