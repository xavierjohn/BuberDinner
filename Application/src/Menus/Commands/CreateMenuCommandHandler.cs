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
        await CreateMenuSections(request.Sections)
            .Bind(sections => Menu.TryCreate(request.Name, request.Description, sections, request.HostId))
            .TapAsync(menu => _menuRepository.Add(menu, cancellationToken));

    private static Result<IReadOnlyList<MenuSection>> CreateMenuSections(IReadOnlyList<MenuSectionCommand> commands) =>
        commands.TraverseAll(command =>
            CreateMenuItems(command.Items)
                .Bind(items => MenuSection.TryCreate(command.Name, command.Description, items)));

    private static Result<IReadOnlyList<MenuItem>> CreateMenuItems(IReadOnlyList<MenuItemCommand> commands) =>
        commands.TraverseAll(command => MenuItem.TryCreate(command.Name, command.Description));
}
