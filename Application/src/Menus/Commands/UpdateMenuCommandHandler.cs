namespace BuberDinner.Application.Menus.Commands;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Menu;
using Mediator;

public sealed class UpdateMenuCommandHandler : IRequestHandler<UpdateMenuCommand, Result<Menu>>
{
    private readonly IRepository<Menu> _menuRepository;

    public UpdateMenuCommandHandler(IRepository<Menu> menuRepository)
    {
        _menuRepository = menuRepository;
    }

    public async ValueTask<Result<Menu>> Handle(UpdateMenuCommand request, CancellationToken cancellationToken) =>
        await LoadMenuAsync(request, cancellationToken)
            .RequireETagAsync(request.IfMatch)
            .BindAsync(menu => menu.Update(request.Name, request.Description))
            .TapAsync(menu => _menuRepository.Update(menu, cancellationToken));

    private async ValueTask<Result<Menu>> LoadMenuAsync(UpdateMenuCommand request, CancellationToken cancellationToken)
    {
        var menu = await _menuRepository.FindById(request.MenuId.Value.ToString(), cancellationToken);
        if (menu is null)
            return Result.Fail<Menu>(new Error.NotFound(ResourceRef.For<Menu>(request.MenuId)));
        if (menu.HostId != request.HostId)
            return Result.Fail<Menu>(new Error.NotFound(ResourceRef.For<Menu>(request.MenuId))
            {
                Detail = "Menu does not belong to the specified host.",
            });
        return Result.Ok(menu);
    }
}
