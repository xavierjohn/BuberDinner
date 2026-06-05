namespace BuberDinner.Application.Menus.Queries;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Menu;
using Mediator;

public sealed class GetMenuQueryHandler : IRequestHandler<GetMenuQuery, Result<Menu>>
{
    private readonly IRepository<Menu> _menuRepository;

    public GetMenuQueryHandler(IRepository<Menu> menuRepository)
    {
        _menuRepository = menuRepository;
    }

    public async ValueTask<Result<Menu>> Handle(GetMenuQuery request, CancellationToken cancellationToken)
    {
        var menu = await _menuRepository.FindById(request.MenuId.Value.ToString(), cancellationToken);
        if (menu is null)
            return Result.Fail<Menu>(new Error.NotFound(ResourceRef.For<Menu>(request.MenuId)));
        // Hierarchical-route membership check: same as UpdateMenuCommandHandler. Returning
        // NotFound (not Forbidden) keeps the response shape symmetric and avoids leaking the
        // existence of menus the caller has no business asking about.
        if (menu.HostId != request.HostId)
            return Result.Fail<Menu>(new Error.NotFound(ResourceRef.For<Menu>(request.MenuId))
            {
                Detail = "Menu does not belong to the specified host.",
            });
        return Result.Ok(menu);
    }
}
