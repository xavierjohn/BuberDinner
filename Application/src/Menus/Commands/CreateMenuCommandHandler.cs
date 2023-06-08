namespace BuberDinner.Application.Menus.Commands;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Menu;
using BuberDinner.Domain.Menu.Entities;
using Mediator;

public class CreateMenuCommandHandler : IRequestHandler<CreateMenuCommand, Result<Menu>>
{
    private readonly IRepository<Menu> _menuRepository;

    public CreateMenuCommandHandler(IRepository<Menu> menuRepository)
    {
        _menuRepository = menuRepository;
    }

    public ValueTask<Result<Menu>> Handle(CreateMenuCommand request, CancellationToken cancellationToken) =>
        CreateMenu(request, cancellationToken);

    private async ValueTask<Result<Menu>> CreateMenu(CreateMenuCommand request, CancellationToken cancellationToken) =>
        await Menu.New(request.Name, request.Description, CreateMenuSections(request.Sections), request.HostId)
        .TeeAsync(menu => _menuRepository.Add(menu, cancellationToken));

    private static IReadOnlyList<MenuSection> CreateMenuSections(IReadOnlyList<MenuSectionCommand> commands) =>
        commands
            .Select(msc => MenuSection.New(msc.Name, msc.Description, CreateMenuItems(msc.Items)).Value)
            .ToList();

    private static IReadOnlyList<MenuItem> CreateMenuItems(IReadOnlyList<MenuItemCommand> commands) =>
        commands
            .Select(mic => MenuItem.New(mic.Name, mic.Description).Value)
            .ToList();
}
