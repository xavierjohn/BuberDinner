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

    public async ValueTask<Result<Menu>> Handle(GetMenuQuery request, CancellationToken cancellationToken) =>
        (await _menuRepository.FindById(request.MenuId.Value.ToString(), cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<Menu>(request.MenuId)))
            .Ensure(
                m => m.HostId == request.HostId,
                new Error.NotFound(ResourceRef.For<Menu>(request.MenuId))
                {
                    Detail = "Menu does not belong to the specified host.",
                });
}
